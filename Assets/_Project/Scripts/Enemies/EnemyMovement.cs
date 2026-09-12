using UnityEngine;
using ScalePunch.Combat;

namespace ScalePunch.Enemies
{
    /// <summary>
    /// Transform-driven chase with decaying knockback. No Rigidbody on purpose:
    /// 150 rigidbodies is the difference between 60 and 25 fps on mid-range
    /// Android, and none of this needs real physics.
    /// </summary>
    public class EnemyMovement : MonoBehaviour
    {
        [Header("Separation")]
        [Tooltip("Cheap crowd spreading so enemies don't stack into one pixel.")]
        [SerializeField] float separationRadius = 0.7f;
        [SerializeField] float separationStrength = 2.5f;
        [Tooltip("Enemies checked per frame for separation. Sampling a slice keeps this O(k), not O(n²).")]
        [SerializeField] int separationSamples = 8;

        [Header("Knockback")]
        [SerializeField] float knockbackDecay = 9f;

        EnemyDefinition _definition;
        Transform _target;
        Health _targetHealth;
        float _damage;
        float _baseDamage;
        float _speedMultiplier = 1f;
        float _attackTimer;
        Vector3 _knockbackVelocity;
        Vector3 _lastPosition;
        int _separationCursor;

        /// <summary>Who this zombie is walking toward. Scripted behaviours (the
        /// boss charge) need it and should not re-find the player themselves.</summary>
        public Transform Target { get; private set; }

        /// <summary>
        /// While true this component stops driving the transform entirely, so a
        /// scripted behaviour can move the body without the two fighting each
        /// other for the same position every frame.
        /// </summary>
        public bool ExternalControl { get; set; }

        public float Damage => _damage;

        /// <summary>
        /// Smoothed ground-plane velocity, for the turret's intercept solve.
        ///
        /// Smoothed rather than raw: a single frame's delta spikes hard during
        /// knockback and separation jitter, and feeding that straight into a
        /// lead calculation makes the gun aim at empty floor.
        /// </summary>
        public Vector3 Velocity { get; private set; }

        public void Configure(EnemyDefinition definition, float damage, Transform target)
        {
            _definition = definition;
            _damage = damage;
            _baseDamage = damage;
            _speedMultiplier = 1f;
            _target = target;
            Target = target;
            _targetHealth = target != null ? target.GetComponent<Health>() : null;
            _attackTimer = 0f;
            _knockbackVelocity = Vector3.zero;
            _lastPosition = transform.position;
            Velocity = Vector3.zero;
            ExternalControl = false;
        }

        /// <summary>Retunes speed and damage mid-life. The boss uses this on its
        /// phase change rather than being respawned as a different definition.</summary>
        public void SetSpeedAndDamage(float speedMultiplier, float damageMultiplier)
        {
            _speedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            _damage = _baseDamage * Mathf.Max(0.1f, damageMultiplier);
        }

        /// <summary>Applies contact damage on demand — the boss charge deals its
        /// own damage rather than waiting for the melee interval.</summary>
        public bool TryDamageTarget(float amount)
        {
            if (_targetHealth == null || _targetHealth.IsDead) return false;

            _targetHealth.TakeDamage(new DamageInfo(amount, false, transform.position, gameObject));
            return true;
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (_definition == null) return;

            float resisted = force * (1f - _definition.knockbackResistance);
            if (resisted <= 0f) return;

            direction.y = 0f;
            _knockbackVelocity += direction.normalized * resisted;
        }

        void Update()
        {
            if (_definition == null || _target == null) return;
            if (ExternalControl) return;

            float dt = Time.deltaTime;
            Vector3 position = transform.position;

            Vector3 toTarget = _target.position - position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            // Knockback overrides pursuit while it is still meaningful.
            if (_knockbackVelocity.sqrMagnitude > 0.01f)
            {
                position += _knockbackVelocity * dt;
                _knockbackVelocity = Vector3.MoveTowards(
                    _knockbackVelocity, Vector3.zero, knockbackDecay * dt);
            }
            else if (distance > _definition.attackRange)
            {
                Vector3 step = toTarget.normalized * (_definition.moveSpeed * _speedMultiplier * dt);
                position += step + Separation() * (separationStrength * dt);
            }

            Vector3 frameVelocity = (position - _lastPosition) / dt;
            frameVelocity.y = 0f;
            _lastPosition = position;

            // ~5 frame smoothing. Enough to shrug off separation jitter, short
            // enough that a Runner changing direction is tracked within a few
            // frames rather than half a second.
            Velocity = Vector3.Lerp(Velocity, frameVelocity, 1f - Mathf.Exp(-12f * dt));

            transform.position = position;

            if (distance > 0.01f)
                transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

            TickAttack(dt, distance);
        }

        void TickAttack(float dt, float distance)
        {
            _attackTimer -= dt;
            if (distance > _definition.attackRange) return;
            if (_attackTimer > 0f) return;
            if (_targetHealth == null || _targetHealth.IsDead) return;

            _targetHealth.TakeDamage(new DamageInfo(_damage, false, transform.position, gameObject));
            _attackTimer = _definition.attackInterval;
        }

        /// <summary>
        /// Pushes away from a rotating sample of neighbours rather than all of
        /// them. Over a few frames every enemy is considered, at a fraction of
        /// the cost, and the crowd still spreads.
        /// </summary>
        Vector3 Separation()
        {
            var all = EnemyRegistry.All;
            if (all.Count <= 1) return Vector3.zero;

            Vector3 push = Vector3.zero;
            Vector3 position = transform.position;
            float radiusSqr = separationRadius * separationRadius;
            int samples = Mathf.Min(separationSamples, all.Count);

            for (int i = 0; i < samples; i++)
            {
                _separationCursor = (_separationCursor + 1) % all.Count;
                Enemy other = all[_separationCursor];
                if (other == null || other.transform == transform) continue;

                Vector3 delta = position - other.transform.position;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr > radiusSqr || sqr < 0.0001f) continue;

                push += delta.normalized * (1f - Mathf.Sqrt(sqr) / separationRadius);
            }
            return push;
        }
    }
}
