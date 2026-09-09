using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Core;

namespace ScalePunch.Enemies
{
    /// <summary>
    /// Ring spawner: enemies always appear just outside the camera's view, never
    /// popping in on screen. M0 ramps difficulty on a timer; M1 replaces the
    /// ramp with WaveDefinition timelines (docs/03-roadmap.md).
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform target;
        [SerializeField] EnemyDefinition[] definitions;

        [Header("Spawn ring")]
        [Tooltip("Distance from the player. Must exceed the camera's visible radius.")]
        [SerializeField] float spawnRadius = 14f;

        [Header("Ramp")]
        [SerializeField] float initialInterval = 1.4f;
        [SerializeField] float minimumInterval = 0.18f;
        [Tooltip("Seconds of run time per difficulty wave step.")]
        [SerializeField] float secondsPerWave = 20f;
        [SerializeField] float stageMultiplier = 1f;

        [Header("Budget")]
        [Tooltip("Hard cap. 150 is the mobile budget from docs/02-tech-stack.md §5.")]
        [SerializeField] int maxConcurrent = 150;
        [SerializeField] int prewarmPerType = 24;

        readonly Dictionary<EnemyDefinition, Pool<Enemy>> _pools = new();
        readonly HashSet<Enemy> _wired = new();
        Transform _poolRoot;
        float _timer;
        float _elapsed;

        public int CurrentWave => Mathf.FloorToInt(_elapsed / secondsPerWave);

        void Awake()
        {
            EnemyRegistry.Clear();

            _poolRoot = new GameObject("~EnemyPool").transform;
            _poolRoot.SetParent(transform, false);

            foreach (EnemyDefinition def in definitions)
            {
                if (def == null || def.prefab == null)
                {
                    Debug.LogError($"[EnemySpawner] Definition '{(def == null ? "null" : def.id)}' has no prefab.", this);
                    continue;
                }
                _pools[def] = new Pool<Enemy>(def.prefab, _poolRoot, prewarmPerType);
            }
        }

        void OnDestroy() => EnemyRegistry.Clear();

        void Update()
        {
            _elapsed += Time.deltaTime;
            _timer -= Time.deltaTime;

            if (_timer > 0f) return;
            _timer = CurrentInterval();

            if (EnemyRegistry.Count >= maxConcurrent) return;
            Spawn();
        }

        float CurrentInterval()
        {
            // Halves roughly every 3 waves, floored so late game stays playable.
            float t = _elapsed / (secondsPerWave * 3f);
            return Mathf.Max(minimumInterval, initialInterval * Mathf.Pow(0.5f, t));
        }

        void Spawn()
        {
            if (_pools.Count == 0 || target == null) return;

            EnemyDefinition def = definitions[Random.Range(0, definitions.Length)];
            if (def == null || !_pools.TryGetValue(def, out Pool<Enemy> pool)) return;

            float angle = Random.value * Mathf.PI * 2f;
            Vector3 offset = new(Mathf.Cos(angle) * spawnRadius, 0f, Mathf.Sin(angle) * spawnRadius);
            Vector3 position = target.position + offset;

            Enemy enemy = pool.Get(position, Quaternion.identity);

            // Wired once per object, never per spawn: a per-spawn lambda would
            // allocate a closure on every single enemy, and re-subscribing
            // without unsubscribing would return the enemy to its pool once per
            // life it had ever lived.
            if (_wired.Add(enemy)) enemy.Despawned += ReturnToPool;

            enemy.Spawn(def, CurrentWave, stageMultiplier, target);
        }

        void ReturnToPool(Enemy enemy)
        {
            if (enemy.Definition != null && _pools.TryGetValue(enemy.Definition, out Pool<Enemy> pool))
                pool.Release(enemy);
        }

        void OnDrawGizmosSelected()
        {
            if (target == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, spawnRadius);
        }
    }
}
