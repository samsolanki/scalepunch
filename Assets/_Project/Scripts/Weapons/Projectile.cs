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
        int _targetGeneration;
        float _damage;
        float _speed;
        float _hitRadius;
        float _steerDegPerSec;
        float _rangeRemaining;
        float _lifeRemaining;
        float _lifestealFraction;
        bool _reserved;
        bool _connected;
        int _shotId;
        bool _guaranteed;
        float _retargetRadius;
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
                           Health shooter = null, float lifestealFraction = 0f,
                           bool guaranteedHit = false, float retargetRadius = 4f)
        {
            transform.position = origin;

            direction.y = 0f;
            _direction = direction.sqrMagnitude < 0.0001f ? Vector3.forward : direction.normalized;
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);

            _target = target;
            _targetGeneration = target != null ? target.Generation : 0;
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

            // Claim this round's damage against the target for as long as it is
            // in the air, so the turret can see the kill coming and move on.
            _reserved = target != null && !target.IsDead;
            if (_reserved) target.ReserveDamage(damage);

            _guaranteed = guaranteedHit;
            _retargetRadius = retargetRadius;
            _connected = false;
            _shotId = WeaponTelemetry.ReportFired(target, origin);

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

            if (_guaranteed) KeepTarget();
            Steer(dt);

            Vector3 from = transform.position;
            float step = _speed * dt;

            // Rounds die at the edge of the engagement radius, so the range stat
            // means exactly what it shows.
            //
            // Except while a guaranteed round still has something to hit: the
            // range gate is about how far the gun reaches, and the target was
            // inside that when the trigger went. Expiring mid-flight because the
            // zombie walked a step would break the guarantee over a technicality.
            bool rangeBound = !_guaranteed || !TargetValid;

            if (rangeBound && step >= _rangeRemaining) { Expire(); return; }
            _rangeRemaining -= step;

            Vector3 to = from + _direction * step;
            Sweep(from, to);
            if (!_live) return;

            transform.position = to;
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);
        }

        /// <summary>
        /// Steers toward a freshly solved intercept, not toward where the target
        /// currently stands.
        ///
        /// Chasing the current position is pursuit guidance, and pursuit always
        /// curves in behind a crossing target. Against the turret's lead it was
        /// worse than useless: the turret aimed at where the zombie would be and
        /// this dragged the round back to where it was, erasing the lead a frame
        /// at a time. Re-solving keeps the round on an interception course as the
        /// target manoeuvres, which is the only thing homing should be doing.
        /// </summary>
        /// <summary>
        /// Hands a round whose target died to the nearest zombie instead of
        /// letting it sail into empty floor. A round with nothing to hit is not a
        /// miss, but it is still a wasted round.
        /// </summary>
        void KeepTarget()
        {
            if (TargetValid) return;
            if (_retargetRadius <= 0f) return;

            Enemy replacement = EnemyRegistry.FindNearestExcluding(
                transform.position, _retargetRadius, _alreadyHit);

            if (replacement == null) return;

            ReleaseReservation();

            _target = replacement;
            _targetGeneration = replacement.Generation;

            _reserved = true;
            replacement.ReserveDamage(_damage);
        }

        void Steer(float dt)
        {
            if (_steerDegPerSec <= 0f) return;
            if (!TargetValid) return;

            Vector3 targetVelocity = _target.Movement != null ? _target.Movement.Velocity : Vector3.zero;
            Vector3 aimPoint = Ballistics.Intercept(transform.position, _target.transform.position,
                                                    targetVelocity, _speed);

            Vector3 toAim = aimPoint - transform.position;
            toAim.y = 0f;
            if (toAim.sqrMagnitude < 0.0001f) return;

            _direction = Vector3.RotateTowards(
                _direction, toAim.normalized,
                _steerDegPerSec * Mathf.Deg2Rad * dt, 0f).normalized;
        }

        /// <summary>Reports whether the round ever touched anything, so a round
        /// spent on an already-dying zombie is not counted as an aiming failure.</summary>
        /// <summary>
        /// The round is still locked onto the same zombie it was fired at.
        ///
        /// The generation check is the whole point: a pooled Enemy that died and
        /// was re-spawned passes `!IsDead` while being an entirely different
        /// zombie somewhere else on the field.
        /// </summary>
        bool TargetValid => _target != null && !_target.IsDead && _target.Generation == _targetGeneration;

        void Sweep(Vector3 from, Vector3 to)
        {
            // Loops so one fast round can pierce several zombies standing in a
            // line within a single frame's travel.
            while (_live)
            {
                Enemy hit = EnemyRegistry.FindFirstAlongSegment(from, to, _hitRadius, _alreadyHit);
                if (hit == null) return;

                _alreadyHit.Add(hit);
                _connected = true;

                // Captured either side of the call so the log shows what the round
                // actually removed, after armour — not the number it set out with.
                float before = hit.Health.Current;
                hit.Health.TakeDamage(new DamageInfo(_damage, _isCrit, from, gameObject));
                WeaponTelemetry.ReportHit(_shotId, hit, before - hit.Health.Current,
                                          before, hit.Health.Current);

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

            // A connected round already reported itself at the moment of impact,
            // which is also the only place the real damage figure exists.
            if (!_connected)
            {
                if (TargetValid) WeaponTelemetry.ReportExpired(_shotId, transform.position, _target, _hitRadius);
                else WeaponTelemetry.ReportWasted(_shotId);
            }

            ReleaseReservation();
            Expired?.Invoke(this);
        }

        /// <summary>
        /// Hands the claim back. Must run on every exit path — a round that dies
        /// without releasing leaves its target permanently looking doomed, and
        /// the turret never shoots it again.
        /// </summary>
        void ReleaseReservation()
        {
            if (!_reserved) return;

            _reserved = false;

            // Only give the claim back to the zombie it was made against. A
            // recycled Enemy is a different zombie, and refunding it damage it
            // never had reservations for corrupts its IsDoomed state.
            if (TargetValid) _target.ReleaseDamage(_damage);
        }

        void OnDisable()
        {
            _live = false;
            ReleaseReservation();
        }
    }
}
