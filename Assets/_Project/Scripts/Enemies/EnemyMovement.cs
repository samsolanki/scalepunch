using UnityEngine;
using ScalePunch.Combat;

namespace ScalePunch.Enemies
{
    /// <summary>
    /// Transform-driven chase with decaying knockback. No Rigidbody on purpose:
    /// 150 rigidbodies is the difference between 60 and 25 fps on mid-range
    /// Android, and none of this needs real physics.
    /// </summary>
    /// <summary>Moves before anything reads its position this frame.</summary>
    [DefaultExecutionOrder(-100)]
    public class EnemyMovement : MonoBehaviour
    {
        [Header("Separation")]
        [Tooltip("Cheap crowd spreading so enemies don't stack into one pixel.")]
        [SerializeField] float separationRadius = 0.7f;
        [SerializeField] float separationStrength = 2.5f;
        [Tooltip("Enemies checked per frame for separation. Sampling a slice keeps this O(k), not O(n²).")]
        [SerializeField] int separationSamples = 8;

        [Header("Standoff")]
        [Tooltip("Ground-plane radius of the player's body. The stop distance is this " +
                 "plus the zombie's own radius plus its standoff gap.")]
        [SerializeField] float playerRadius = 0.5f;
        [Tooltip("Extra reach past the standoff ring within which the zombie can swing.")]
        [SerializeField] float attackReach = 0.2f;

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
        /// How close this zombie may get, centre to centre.
        ///
        /// Derived rather than a flat number, so every tier stops with the same
        /// visible gap: a Boss at 2.2 scale carries a 1.1 m body and stands
        /// further out than a Runner instead of clipping halfway through
        /// the player.
        /// </summary>
        public float StopDistance { get; private set; } = 1f;

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

            // 0.5 is Unity's capsule radius at scale 1.
            float bodyRadius = 0.5f * Mathf.Max(0.01f, definition.scale);
            StopDistance = playerRadius + bodyRadius + Mathf.Max(0f, definition.standoffGap);
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

        static bool IsFinite(Vector3 v) =>
            !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
              float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

        void Update()
        {
            if (_definition == null || _target == null) return;
            if (ExternalControl) return;

            float dt = Time.deltaTime;

            // timeScale is exactly 0 while the level-up draft holds the game, so
            // the frame delta below would be a divide by zero. That matters more
            // than it looks: Vector3.Lerp computes a + (b - a) * t, so a single
            // NaN frameVelocity poisons Velocity permanently - t = 0 does not
            // discard it, and every later Lerp carries the NaN forward. The
            // turret then leads on a NaN intercept and both its aim and its
            // rounds go wild until that zombie happens to die and a pooled
            // replacement resets the field.
            //
            // Hitstop is unaffected either way: it sets timeScale to 0.05, not 0.
            if (dt <= 0f) return;

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
            else if (distance > StopDistance)
            {
                Vector3 step = toTarget.normalized * (_definition.moveSpeed * _speedMultiplier * dt);
                position += step + Separation() * (separationStrength * dt);
            }
            else
            {
                // On the ring already: still spread sideways against neighbours,
                // but never advance. Without this the back of a crowd keeps
                // pushing and the front rank is driven through the player.
                position += Separation() * (separationStrength * dt);
            }

            // Hard clamp, applied after everything — pursuit, separation and
            // knockback alike.
            //
            // Separation pushes a zombie away from its neighbours, and in a crowd
            // pressed against the player the only free direction is straight
            // through them. That is how a dozen zombies ended up stacked at 0.1 m,
            // inside the body they were supposed to be attacking. A condition on
            // the pursuit step cannot prevent it; only a clamp on the final
            // position can.
            position = ClampOutside(position, _target.position, StopDistance);

            Vector3 frameVelocity = (position - _lastPosition) / dt;
            frameVelocity.y = 0f;
            _lastPosition = position;

            // ~5 frame smoothing. Enough to shrug off separation jitter, short
            // enough that a Runner changing direction is tracked within a few
            // frames rather than half a second.
            Vector3 smoothed = Vector3.Lerp(Velocity, frameVelocity, 1f - Mathf.Exp(-12f * dt));

            // Belt and braces against the same class of fault. A NaN here is
            // self-perpetuating and silently corrupts aiming for every shot at
            // this target, so it is worth never letting one survive a frame.
            Velocity = IsFinite(smoothed) ? smoothed : Vector3.zero;

            transform.position = position;

            if (distance > 0.01f)
                transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

            TickAttack(dt, distance);
        }

        /// <summary>Pushes a position back out to the standoff ring if it has
        /// crossed inside it, keeping its bearing.</summary>
        static Vector3 ClampOutside(Vector3 position, Vector3 centre, float minDistance)
        {
            Vector3 offset = position - centre;
            float height = offset.y;
            offset.y = 0f;

            float distance = offset.magnitude;
            if (distance >= minDistance) return position;

            // Exactly on top: pick an arbitrary bearing rather than divide by zero.
            Vector3 bearing = distance < 0.0001f ? Vector3.forward : offset / distance;

            Vector3 pushed = centre + bearing * minDistance;
            pushed.y = centre.y + height;
            return pushed;
        }

        void TickAttack(float dt, float distance)
        {
            _attackTimer -= dt;
            if (distance > StopDistance + attackReach) return;
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
