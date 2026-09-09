using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Enemies;

namespace ScalePunch.Weapons
{
    /// <summary>
    /// One round. Travels, steers gently toward the zombie it was fired at,
    /// and damages what it sweeps through. Hit detection goes through
    /// EnemyRegistry rather than physics, so rounds need no colliders and no
    /// rigidbody — consistent with the rest of combat.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] TrailRenderer trail;

        Vector3 _direction;
        Enemy _target;
        float _damage;
        float _speed;
        float _hitRadius;
        float _steerDegPerSec;
        float _rangeRemaining;
        float _lifeRemaining;
        float _lifestealFraction;
        Health _shooter;
        bool _isCrit;
        int _pierceRemaining;
        bool _live;

        readonly List<Enemy> _alreadyHit = new(4);

        /// <summary>Raised when the round is spent, so the pool can reclaim it.</summary>
        public System.Action<Projectile> Expired;

        public void Launch(Vector3 origin, Vector3 direction, Enemy target,
                           float damage, bool isCrit, float speed, float hitRadius,
                           int pierce, float range, float lifetime, float steerDegPerSec,
                           Health shooter = null, float lifestealFraction = 0f)
        {
            transform.position = origin;

            direction.y = 0f;
            _direction = direction.sqrMagnitude < 0.0001f ? Vector3.forward : direction.normalized;
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);

            _target = target;
            _damage = damage;
            _isCrit = isCrit;
            _speed = speed;
            _hitRadius = hitRadius;
            _pierceRemaining = pierce;
            _rangeRemaining = range;
            _lifeRemaining = lifetime;
            _steerDegPerSec = steerDegPerSec;
            _shooter = shooter;
            _lifestealFraction = lifestealFraction;
            _live = true;

            _alreadyHit.Clear();
            if (trail != null) trail.Clear();
        }

        void Update()
        {
            if (!_live) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;   // hitstop — rounds hang in the air with everything else

            _lifeRemaining -= dt;
            if (_lifeRemaining <= 0f) { Expire(); return; }

            Steer(dt);

            Vector3 from = transform.position;
            float step = _speed * dt;

            // Rounds die at the edge of the engagement radius, not somewhere
            // vague off screen, so the range stat means exactly what it shows.
            if (step >= _rangeRemaining) { Expire(); return; }
            _rangeRemaining -= step;

            Vector3 to = from + _direction * step;
            Sweep(from, to);
            if (!_live) return;

            transform.position = to;
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);
        }

        void Steer(float dt)
        {
            if (_steerDegPerSec <= 0f) return;
            if (_target == null || _target.IsDead) return;

            Vector3 toTarget = _target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f) return;

            _direction = Vector3.RotateTowards(
                _direction, toTarget.normalized,
                _steerDegPerSec * Mathf.Deg2Rad * dt, 0f).normalized;
        }

        void Sweep(Vector3 from, Vector3 to)
        {
            // Loops so one fast round can pierce several zombies standing in a
            // line within a single frame's travel.
            while (_live)
            {
                Enemy hit = EnemyRegistry.FindFirstAlongSegment(from, to, _hitRadius, _alreadyHit);
                if (hit == null) return;

                _alreadyHit.Add(hit);
                hit.Health.TakeDamage(new DamageInfo(_damage, _isCrit, from, gameObject));

                // Lifesteal resolves at the point of impact, not the point of
                // firing — a round in flight has not healed anyone yet.
                if (_lifestealFraction > 0f && _shooter != null)
                    _shooter.Heal(_damage * _lifestealFraction);

                if (_pierceRemaining <= 0) { Expire(); return; }
                _pierceRemaining--;
            }
        }

        void Expire()
        {
            if (!_live) return;
            _live = false;
            Expired?.Invoke(this);
        }

        void OnDisable() => _live = false;
    }
}
