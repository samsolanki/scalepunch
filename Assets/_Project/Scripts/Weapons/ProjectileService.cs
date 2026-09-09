using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Core;
using ScalePunch.Enemies;

namespace ScalePunch.Weapons
{
    /// <summary>
    /// Pools rounds per prefab. At 3 shots/sec with pierce and multishot this is
    /// the highest-churn object in the game — it must never Instantiate mid-run.
    /// </summary>
    public class ProjectileService : MonoSingleton<ProjectileService>
    {
        [SerializeField] int prewarmPerPrefab = 48;

        readonly Dictionary<Projectile, Pool<Projectile>> _pools = new();
        /// <summary>Live round -> the pool that owns it. Doubles as the
        /// "already wired" set, so each round subscribes exactly once.</summary>
        readonly Dictionary<Projectile, Pool<Projectile>> _owner = new();
        Transform _root;

        protected override void Awake()
        {
            base.Awake();
            _root = new GameObject("~ProjectilePool").transform;
            _root.SetParent(transform, false);
        }

        public void Fire(WeaponDefinition weapon, Vector3 origin, Vector3 direction, Enemy target,
                         float damage, bool isCrit, float speed, int pierce, float range)
        {
            if (weapon == null || weapon.projectilePrefab == null) return;

            // LookRotation logs an error and returns identity on a zero vector.
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.000001f) return;
            direction.Normalize();

            Pool<Projectile> pool = PoolFor(weapon.projectilePrefab);
            Projectile round = pool.Get(origin, Quaternion.LookRotation(direction, Vector3.up));

            // Wired once per object, not per shot: a per-shot subscription would
            // allocate, and would return the round to the pool once for every
            // shot it had ever been part of.
            if (!_owner.ContainsKey(round))
            {
                _owner[round] = pool;
                round.Expired += Reclaim;
            }

            round.Launch(origin, direction, target, damage, isCrit, speed,
                         weapon.hitRadius, pierce, range, weapon.lifetime,
                         weapon.steerDegreesPerSecond);
        }

        Pool<Projectile> PoolFor(Projectile prefab)
        {
            if (_pools.TryGetValue(prefab, out Pool<Projectile> pool)) return pool;

            pool = new Pool<Projectile>(prefab, _root, prewarmPerPrefab);
            _pools[prefab] = pool;
            return pool;
        }

        void Reclaim(Projectile round)
        {
            if (_owner.TryGetValue(round, out Pool<Projectile> pool)) pool.Release(round);
        }
    }
}
