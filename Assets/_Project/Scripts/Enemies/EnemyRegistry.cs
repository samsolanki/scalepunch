using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScalePunch.Enemies
{
    /// <summary>
    /// Every live enemy, in one list. All "nearest enemy" and "enemies in
    /// range" queries go through here instead of Physics — no colliders, no
    /// rigidbodies, no layer setup, and no per-frame allocation.
    ///
    /// A linear scan is correct at M0 scale: queries happen on attack
    /// cooldowns, not per frame, so it is a few hundred distance checks per
    /// second against a 150-enemy cap. Swap the backing store for a uniform
    /// spatial grid at M3 if the profiler asks for it — the API here does not
    /// need to change.
    /// </summary>
    public static class EnemyRegistry
    {
        static readonly List<Enemy> Active = new(256);

        /// <summary>Raised on every kill. Static, so anything in the run can count
        /// them without holding a reference to each zombie.</summary>
        public static event Action<Enemy> Killed;

        public static void ReportKill(Enemy enemy) => Killed?.Invoke(enemy);

        public static IReadOnlyList<Enemy> All => Active;
        public static int Count => Active.Count;

        public static void Register(Enemy enemy)
        {
            if (!Active.Contains(enemy)) Active.Add(enemy);
        }

        public static void Unregister(Enemy enemy) => Active.Remove(enemy);

        /// <summary>Scene teardown — statics outlive scene loads, so the Run
        /// scene must clear this or the next run starts with ghosts.</summary>
        /// <summary>
        /// Scene teardown. Clears the roster only — NOT the Killed subscribers.
        ///
        /// Nulling the event here was an ordering bug: Unity does not define
        /// whether a listener's OnEnable runs before or after the spawner's
        /// Awake, so a subscriber that happened to register first had its
        /// subscription silently wiped and its counter stayed at zero forever.
        /// Subscribers unsubscribe in OnDisable, which is deterministic.
        /// </summary>
        public static void Clear() => Active.Clear();

        public static Enemy FindNearest(Vector3 position, float maxDistance)
        {
            float bestSqr = maxDistance * maxDistance;
            Enemy best = null;

            for (int i = 0; i < Active.Count; i++)
            {
                Enemy e = Active[i];
                if (e == null || e.IsDead) continue;

                float sqr = (e.transform.position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = e;
                }
            }
            return best;
        }

        /// <summary>
        /// First zombie whose body intersects the swept segment a projectile
        /// covered this frame, nearest to <paramref name="from"/> first.
        ///
        /// Sweeping rather than point-testing matters: a 30 m/s round covers
        /// half a metre per frame, so a point test at the round's new position
        /// would punch straight through a zombie that was standing between the
        /// two positions.
        /// </summary>
        public static Enemy FindFirstAlongSegment(Vector3 from, Vector3 to, float radius,
                                                  List<Enemy> ignore = null)
        {
            float bestT = float.MaxValue;
            Enemy best = null;

            for (int i = 0; i < Active.Count; i++)
            {
                Enemy e = Active[i];
                if (e == null || e.IsDead) continue;
                if (ignore != null && ignore.Contains(e)) continue;

                // The round's radius PLUS the target's own, which is a sphere-vs-
                // capsule sweep rather than a point test against a fixed number.
                // A single flat radius is only ever right for one tier, and every
                // other tier either eats phantom hits or shrugs off real ones.
                float reach = radius + e.BodyRadius;

                float sqr = SqrDistanceToSegment(e.transform.position, from, to, out float t);
                if (sqr > reach * reach || t >= bestT) continue;

                bestT = t;
                best = e;
            }
            return best;
        }

        /// <summary>Squared distance from a point to a segment, with the
        /// normalised position along that segment of the closest point.</summary>
        /// <summary>
        /// Distance measured on the ground plane only.
        ///
        /// Height is decorative in a top-down game and must not be part of hit
        /// detection: rounds leave the muzzle around head height while a zombie's
        /// transform sits on the floor, so a 3D measurement is never smaller than
        /// that vertical gap and nothing can ever be hit.
        /// </summary>
        static float SqrDistanceToSegment(Vector3 point, Vector3 a, Vector3 b, out float t)
        {
            point.y = 0f;
            a.y = 0f;
            b.y = 0f;

            Vector3 ab = b - a;
            float abSqr = ab.sqrMagnitude;

            t = abSqr < 0.000001f ? 0f : Mathf.Clamp01(Vector3.Dot(point - a, ab) / abSqr);
            return (point - (a + ab * t)).sqrMagnitude;
        }

        /// <summary>Non-allocating radius query. Results are appended, not cleared.</summary>
        public static void FindInRadius(Vector3 origin, float radius, List<Enemy> results)
        {
            float radiusSqr = radius * radius;

            for (int i = 0; i < Active.Count; i++)
            {
                Enemy e = Active[i];
                if (e == null || e.IsDead) continue;

                Vector3 delta = e.transform.position - origin;
                delta.y = 0f;
                if (delta.sqrMagnitude <= radiusSqr) results.Add(e);
            }
        }

        /// <summary>
        /// Nearest live zombie not already in <paramref name="exclude"/>.
        /// Used by chaining effects to hop outward without doubling back.
        /// </summary>
        public static Enemy FindNearestExcluding(Vector3 position, float maxDistance, List<Enemy> exclude)
        {
            float bestSqr = maxDistance * maxDistance;
            Enemy best = null;

            for (int i = 0; i < Active.Count; i++)
            {
                Enemy e = Active[i];
                if (e == null || e.IsDead) continue;
                if (exclude != null && exclude.Contains(e)) continue;

                float sqr = (e.transform.position - position).sqrMagnitude;
                if (sqr >= bestSqr) continue;

                bestSqr = sqr;
                best = e;
            }
            return best;
        }

        /// <summary>Non-allocating cone query. Results are appended, not cleared.</summary>
        public static void FindInCone(Vector3 origin, Vector3 forward, float range,
                                      float halfAngleDeg, List<Enemy> results)
        {
            float rangeSqr = range * range;
            float cosHalf = Mathf.Cos(halfAngleDeg * Mathf.Deg2Rad);
            forward.y = 0f;
            forward.Normalize();

            for (int i = 0; i < Active.Count; i++)
            {
                Enemy e = Active[i];
                if (e == null || e.IsDead) continue;

                Vector3 delta = e.transform.position - origin;
                delta.y = 0f;
                if (delta.sqrMagnitude > rangeSqr) continue;

                // A target overlapping the origin has no meaningful direction;
                // always count it as hit rather than dividing by ~zero.
                if (delta.sqrMagnitude < 0.0001f)
                {
                    results.Add(e);
                    continue;
                }

                if (Vector3.Dot(delta.normalized, forward) >= cosHalf) results.Add(e);
            }
        }
    }
}
