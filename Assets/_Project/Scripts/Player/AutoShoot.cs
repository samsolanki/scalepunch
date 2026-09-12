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
        [Tooltip("Floor on the firing arc. Without one, a distant target's angular " +
                 "size shrinks below the turret's ability to settle and it never fires.")]
        [Range(0.25f, 10f)] [SerializeField] float minFiringArc = 1.5f;
        [Tooltip("Ceiling on the firing arc, for targets close enough to be huge on screen.")]
        [Range(5f, 90f)] [SerializeField] float maxFiringArc = 25f;
        [Tooltip("Cap on predicted lead time, in seconds. A target whose intercept is " +
                 "further out than this is not worth leading — it will have changed " +
                 "direction by then.")]
        [SerializeField] float maxLeadSeconds = 1f;
        [Tooltip("Skip zombies with enough damage already in flight to kill them. " +
                 "Turning this off makes the turret dump its whole magazine into the " +
                 "first thing it sees.")]
        [SerializeField] bool avoidOverkill = true;
        [Tooltip("Multiplier on the retention radius. Holding the target until it " +
                 "dies or leaves range is what stops the turret twitching between " +
                 "equidistant zombies — that alone needs no margin.\n\n" +
                 "Keep this at 1. Above it, the turret keeps tracking a target that " +
                 "has drifted past the edge of the ring, and since a round expires " +
                 "at the ring it can neither hit that target nor pick a closer one: " +
                 "the gun simply stops firing.")]
        [Range(1f, 1.5f)] [SerializeField] float targetStickiness = 1f;

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

        /// <summary>Raised once per shot, after the rounds leave the muzzle.
        /// Muzzle flash and recoil hang off this rather than polling.</summary>
        public event System.Action Fired;

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

            bool onTarget = SlewToTarget(Range, out Vector3 aimPoint);
            if (_cooldown > 0f || !onTarget) return;

            Fire(aimPoint);
            _cooldown = interval;
        }

        void AcquireTarget()
        {
            float range = Range;

            // Keep the current target while it is alive and in range.
            //
            // Deliberately NOT dropped for being doomed. Doing that broke exactly
            // one tier: a Shambler has 5 HP and a round carries 5 damage, so a
            // single round in the air marked it dead-on-arrival and the turret
            // stopped tracking it. If that round then missed — separation jitter,
            // knockback, a slightly stale lead — the Shambler walked in unengaged
            // until the round expired. Nothing above 5 HP could reproduce it.
            if (CurrentTarget != null && !CurrentTarget.IsDead)
            {
                float distSqr = (CurrentTarget.transform.position - transform.position).sqrMagnitude;
                if (distSqr <= range * range * targetStickiness) return;
            }

            CurrentTarget = SelectTarget(range);
        }

        bool IsDoomed(Enemy enemy) => avoidOverkill && enemy.IsDoomed;

        /// <summary>
        /// Prefers a target that is not already dead on arrival, but falls back to
        /// one that is rather than returning nothing.
        ///
        /// Doomed is a preference, not an exclusion. As a hard filter it could
        /// leave the turret idle with live zombies inside the ring — which is a
        /// far worse failure than the wasted round it was trying to save.
        /// </summary>
        Enemy SelectTarget(float range)
        {
            Enemy preferred = Scan(range, skipDoomed: true);
            return preferred != null ? preferred : Scan(range, skipDoomed: false);
        }

        /// <summary>
        /// One scan shared by every priority, so the filter cannot apply to three
        /// of the four modes and silently not the fourth.
        /// </summary>
        Enemy Scan(float range, bool skipDoomed)
        {
            var all = EnemyRegistry.All;
            float rangeSqr = range * range;
            Enemy best = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < all.Count; i++)
            {
                Enemy e = all[i];
                if (e == null || e.IsDead) continue;
                if (skipDoomed && IsDoomed(e)) continue;

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

        /// <summary>
        /// Turns the turret toward where the target is *going to be*, and reports
        /// whether a shot fired now would actually connect.
        ///
        /// Three separate things had to be true for a round to be wasted, and all
        /// three were:
        ///
        /// 1. The turret aimed at the target's current position and left the rest
        ///    to the round's weak homing. That is pursuit, not interception, and
        ///    pursuit always trails a crossing target — a Runner at 4.5 m/s moves
        ///    0.9 m during a 9 m flight, against a 0.55 m hit radius.
        /// 2. The firing arc was a flat 12 degrees. At 9 m that is 1.9 m of lateral
        ///    error allowed against a target half a metre wide.
        /// 3. Target stickiness let a zombie sit 7% beyond the engagement radius
        ///    while rounds were still given exactly that radius as their range
        ///    budget — so every shot at a sticky target expired short of it.
        /// </summary>
        bool SlewToTarget(float range, out Vector3 aimPoint)
        {
            // Solved from the turret's pivot, never from the muzzle.
            //
            // The muzzle sits 1.16 m out along the barrel and swings on that
            // radius as the turret turns, while zombies attack from 1.4 m. Once
            // one closes inside that, the muzzle can end up level with or past it
            // and the muzzle-to-target vector flips — the turret whips 180 degrees
            // and the shot leaves backwards. Rotating about the pivot and merely
            // *spawning* at the barrel tip is how a turret actually works, and it
            // has no degenerate case.
            Vector3 origin = turret.position;
            Vector3 targetPosition = CurrentTarget.transform.position;
            Vector3 targetVelocity = CurrentTarget.Movement != null
                ? CurrentTarget.Movement.Velocity
                : Vector3.zero;

            float speed = weapon != null ? weapon.SpeedFor(stats.Stats) : stats.Get(StatType.ProjectileSpeed);
            aimPoint = Ballistics.Intercept(origin, targetPosition, targetVelocity, speed, maxLeadSeconds);

            Vector3 toAim = aimPoint - origin;
            toAim.y = 0f;
            if (toAim.sqrMagnitude < 0.0001f) return false;

            Quaternion desired = Quaternion.LookRotation(toAim.normalized, Vector3.up);
            turret.rotation = Quaternion.RotateTowards(
                turret.rotation, desired, turnSpeed * Time.deltaTime);

            // A round cannot be fired past the edge of the ring, so an intercept
            // beyond it is a guaranteed miss. Hold fire and keep tracking.
            float distance = toAim.magnitude;
            if (distance > range) return false;

            // The arc is the target's angular size, not a fixed number: half a
            // metre of body is 3.5 degrees at 9 m and 15 at 2 m, and one constant
            // cannot be right at both.
            float hitRadius = weapon != null ? weapon.hitRadius : 0.5f;
            float angularSize = Mathf.Atan2(hitRadius, Mathf.Max(0.01f, distance)) * Mathf.Rad2Deg;

            // Plus part of what the round can steer out during its flight. Demanding
            // a perfect alignment the round does not need costs firing time for
            // nothing; taking only half the budget leaves the rest as margin for
            // the target changing direction mid-flight.
            float steerBudget = weapon != null && speed > 0.01f
                ? weapon.steerDegreesPerSecond * (distance / speed) * 0.5f
                : 0f;

            float allowed = Mathf.Clamp(angularSize + steerBudget, minFiringArc, maxFiringArc);

            return Quaternion.Angle(turret.rotation, desired) <= allowed;
        }

        void Fire(Vector3 aimPoint)
        {
            if (weapon == null || !ProjectileService.Exists) return;

            StatSheet sheet = stats.Stats;

            float damage = weapon.DamageFor(sheet);
            float speed = weapon.SpeedFor(sheet);
            float range = Range;
            int pierce = Mathf.RoundToInt(sheet.Get(StatType.Pierce));
            int rounds = RoundsThisShot(sheet);

            // Spawns at the barrel tip, but flies along the pivot-to-intercept
            // line the turret was aimed down — the same line, once the slew has
            // settled, and free of the close-range flip that using the muzzle as
            // the origin introduces.
            Vector3 origin = muzzle.position;

            Vector3 aim = aimPoint - turret.position;
            aim.y = 0f;
            aim = aim.sqrMagnitude < 0.0001f ? turret.forward : aim.normalized;

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

            Fired?.Invoke();
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
