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

        public static IReadOnlyList<Enemy> All => Active;
        public static int Count => Active.Count;

        public static void Register(Enemy enemy)
        {
            if (!Active.Contains(enemy)) Active.Add(enemy);
        }

        public static void Unregister(Enemy enemy) => Active.Remove(enemy);

        /// <summary>Scene teardown — statics outlive scene loads, so the Run
        /// scene must clear this or the next run starts with ghosts.</summary>
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
