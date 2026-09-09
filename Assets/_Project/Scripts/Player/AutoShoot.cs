using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Data;
using ScalePunch.Enemies;
using ScalePunch.Feedback;
using ScalePunch.Weapons;

namespace ScalePunch.Player
{
    /// <summary>
    /// The core verb. The player is a fixed emplacement: it never moves, it
    /// acquires the best zombie inside its engagement radius, turns to face it,
    /// and fires on a cooldown. Entirely automatic — there is no fire button and
    /// no aiming input (docs/01-game-design.md §2).
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class AutoShoot : MonoBehaviour
    {
        public enum TargetPriority
        {
            /// <summary>Nearest zombie. The safe default — it kills what is
            /// about to reach you.</summary>
            Closest,
            /// <summary>Zombie furthest along toward you. Reads as smarter but
            /// lets flankers close in.</summary>
            MostAdvanced,
            /// <summary>Lowest absolute HP. Maximises kills per second, which
            /// is usually wrong when a Brute is the actual threat.</summary>
            Weakest,
            /// <summary>Highest max HP. Good against Brute waves.</summary>
            Toughest
        }

        [Header("References")]
        [SerializeField] PlayerStats stats;
        [SerializeField] WeaponDefinition weapon;
        [SerializeField] CombatTuning tuning;
        [Tooltip("The visual that rotates to face the target.")]
        [SerializeField] Transform turret;
        [Tooltip("Where rounds spawn. A child of the turret, at barrel height.")]
        [SerializeField] Transform muzzle;

        [Header("Targeting")]
        [SerializeField] TargetPriority priority = TargetPriority.Closest;
        [Tooltip("Degrees per second the turret slews. The gun does not fire until it is on target.")]
        [SerializeField] float turnSpeed = 720f;
        [Tooltip("How far off-aim the turret may be and still fire, in degrees.")]
        [Range(1f, 90f)] [SerializeField] float firingArc = 12f;
        [Tooltip("Re-picking a target every frame makes the turret twitch between " +
                 "equidistant zombies. It holds its target until the target dies or leaves this radius.")]
        [SerializeField] float targetStickiness = 1.15f;

        [Header("Debug")]
        [SerializeField] bool drawGizmos = true;

        float _cooldown;
        Health _health;

        /// <summary>Settable at runtime — the HUD toggle drives this.</summary>
        public TargetPriority Priority
        {
            get => priority;
            set
            {
                if (priority == value) return;

                priority = value;
                CurrentTarget = null;   // re-acquire immediately under the new rule
            }
        }

        public Enemy CurrentTarget { get; private set; }
        /// <summary>The engagement radius, after weapon multipliers. Read by RangeIndicator.</summary>
        public float Range => weapon != null ? weapon.RangeFor(stats.Stats) : stats.Get(StatType.Range);
        /// <summary>0-1 progress toward the next shot, for UI.</summary>
        public float ReloadProgress { get; private set; }

        void Reset()
        {
            stats = GetComponent<PlayerStats>();
            turret = transform;
        }

        void Awake()
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (turret == null) turret = transform;
            if (muzzle == null) muzzle = turret;
            _health = GetComponent<Health>();
        }

        void Update()
        {
            float fireRate = weapon != null ? weapon.FireRateFor(stats.Stats) : stats.Get(StatType.FireRate);
            float interval = 1f / Mathf.Max(0.05f, fireRate);

            _cooldown -= Time.deltaTime;
            ReloadProgress = Mathf.Clamp01(1f - _cooldown / interval);

            AcquireTarget();
            if (CurrentTarget == null) return;

            bool onTarget = SlewToTarget();
            if (_cooldown > 0f || !onTarget) return;

            Fire();
            _cooldown = interval;
        }

        void AcquireTarget()
        {
            float range = Range;

            // Keep the current target while it is alive and still roughly in
            // range — the stickiness margin stops the turret oscillating between
            // two zombies at nearly identical distance.
            if (CurrentTarget != null && !CurrentTarget.IsDead)
            {
                float distSqr = (CurrentTarget.transform.position - transform.position).sqrMagnitude;
                if (distSqr <= range * range * targetStickiness) return;
            }

            CurrentTarget = priority == TargetPriority.Closest
                ? EnemyRegistry.FindNearest(transform.position, range)
                : SelectByPriority(range);
        }

        Enemy SelectByPriority(float range)
        {
            var all = EnemyRegistry.All;
            float rangeSqr = range * range;
            Enemy best = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < all.Count; i++)
            {
                Enemy e = all[i];
                if (e == null || e.IsDead) continue;

                float distSqr = (e.transform.position - transform.position).sqrMagnitude;
                if (distSqr > rangeSqr) continue;

                float score = priority switch
                {
                    TargetPriority.MostAdvanced => -distSqr,
                    TargetPriority.Weakest => -e.Health.Current,
                    TargetPriority.Toughest => e.Health.Max,
                    _ => -distSqr
                };

                if (score <= bestScore) continue;
                bestScore = score;
                best = e;
            }
            return best;
        }

        /// <summary>Turns the turret toward the target. Returns true once the
        /// aim error is inside the firing arc.</summary>
        bool SlewToTarget()
        {
            Vector3 toTarget = CurrentTarget.transform.position - turret.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f) return false;

            Quaternion desired = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            turret.rotation = Quaternion.RotateTowards(
                turret.rotation, desired, turnSpeed * Time.deltaTime);

            return Quaternion.Angle(turret.rotation, desired) <= firingArc;
        }

        void Fire()
        {
            if (weapon == null || !ProjectileService.Exists) return;

            StatSheet sheet = stats.Stats;

            float damage = weapon.DamageFor(sheet);
            float speed = weapon.SpeedFor(sheet);
            float range = Range;
            int pierce = Mathf.RoundToInt(sheet.Get(StatType.Pierce));
            int rounds = RoundsThisShot(sheet);

            Vector3 origin = muzzle.position;
            Vector3 aim = turret.forward;

            for (int i = 0; i < rounds; i++)
            {
                // Crit per round, not per shot: a shotgun blast where two of six
                // pellets crit reads far better than an all-or-nothing volley.
                bool isCrit = Random.value < sheet.Get(StatType.CritChance);
                float amount = damage * (isCrit ? sheet.Get(StatType.CritMultiplier) : 1f);

                ProjectileService.Instance.Fire(
                    weapon, origin, SpreadDirection(aim, i, rounds), CurrentTarget,
                    amount, isCrit, speed, pierce, range,
                    _health, sheet.Get(StatType.Lifesteal));
            }

            if (tuning != null && CameraShake.Exists)
                CameraShake.Instance.Shake(tuning.shakeOnFire, tuning.shakeOnFireDuration);
        }

        int RoundsThisShot(StatSheet sheet)
        {
            float count = sheet.Get(StatType.ProjectileCount) + (weapon != null ? weapon.extraProjectiles : 0);

            // A fractional count is the chance of an extra round, so "+0.5
            // projectiles" from an upgrade is worth something immediately
            // instead of rounding away to nothing.
            int whole = Mathf.FloorToInt(count);
            if (Random.value < count - whole) whole++;

            return Mathf.Max(1, whole);
        }

        Vector3 SpreadDirection(Vector3 aim, int index, int total)
        {
            float angle = 0f;

            if (total > 1 && weapon.spreadDegrees > 0f)
            {
                // Fan the volley evenly across the cone, centred on the aim line.
                float t = total == 1 ? 0.5f : index / (float)(total - 1);
                angle = Mathf.Lerp(-weapon.spreadDegrees * 0.5f, weapon.spreadDegrees * 0.5f, t);
            }

            if (weapon.inaccuracyDegrees > 0f)
                angle += Random.Range(-weapon.inaccuracyDegrees, weapon.inaccuracyDegrees);

            return Quaternion.Euler(0f, angle, 0f) * aim;
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmos || !Application.isPlaying || stats == null) return;

            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
            DrawCircle(transform.position, Range, 48);

            if (CurrentTarget == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(muzzle.position, CurrentTarget.transform.position);
        }

        static void DrawCircle(Vector3 centre, float radius, int segments)
        {
            Vector3 previous = centre + Vector3.forward * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector3 next = centre + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
