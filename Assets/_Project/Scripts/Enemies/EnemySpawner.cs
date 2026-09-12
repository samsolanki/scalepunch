using System;
using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Core;
using ScalePunch.Progression;
using ScalePunch.Stages;

namespace ScalePunch.Enemies
{
    /// <summary>
    /// Runs a stage: its waves in order, then its boss.
    ///
    /// Replaces M0's plain timer ramp. A timeline is what lets a wave be
    /// *designed* — a lull before a swarm, a Brute arriving under cover of
    /// runners — none of which a spawn-rate curve can express.
    ///
    /// Enemies always appear on a ring outside the camera, never in view, and
    /// always outside the engagement radius so the player gets the approach.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        enum Phase { Idle, Waves, BossIntro, Boss, Complete }

        [Header("References")]
        [SerializeField] StageDefinition stage;
        [Tooltip("Drives the endless prototype: which tiers are in the mix, their " +
                 "shares, the spawn interval and the burst size, all per wave.")]
        [SerializeField] SpawnRamp ramp;
        [SerializeField] Transform target;
        [Tooltip("Difficulty follows the player's level. Leave empty to fall back to " +
                 "the clock via secondsPerWave.")]
        [SerializeField] LevelSystem levelSource;

        [Header("Spawn ring")]
        [Tooltip("Distance from the player. Must exceed both the camera's visible " +
                 "radius and the player's engagement radius.")]
        [SerializeField] float spawnRadius = 16f;

        [Header("Prototype (waves off)")]
        [Tooltip("Fallback only, used when no LevelSystem is wired: seconds of run " +
                 "time per level.")]
        [SerializeField] float secondsPerWave = 20f;

        [Header("Budget")]
        [Tooltip("Hard cap. 150 is the mobile budget from docs/02-tech-stack.md §5.")]
        [SerializeField] int maxConcurrent = 150;
        [SerializeField] int prewarmPerType = 24;

        readonly Dictionary<EnemyDefinition, Pool<Enemy>> _pools = new();
        readonly HashSet<Enemy> _wired = new();
        readonly List<PendingSpawn> _pending = new(64);

        Transform _poolRoot;
        Phase _phase = Phase.Idle;
        bool _useWaves;
        bool _useBoss;
        float _elapsed;
        float _endlessTimer;
        int _waveIndex = -1;
        int _cursor;
        float _waveElapsed;
        float _phaseTimer;
        int _lastAnnouncedWave;
        Enemy _boss;

        /// <summary>Wave index (0-based) and total, as each wave begins.</summary>
        public event Action<int, int> WaveStarted;
        public event Action<Enemy> BossSpawned;
        /// <summary>The stage is beaten: every wave ran and the boss is dead (or
        /// there was no boss and the field is clear).</summary>
        public event Action StageCleared;

        public StageDefinition Stage => stage;
        /// <summary>1-based, for display. In endless mode it counts up forever.</summary>
        public int CurrentWaveNumber => _useWaves ? _waveIndex + 1 : EndlessWave;
        public int TotalWaves => stage != null ? stage.WaveCount : 0;
        public bool IsBossActive => _phase == Phase.Boss && _boss != null && !_boss.IsDead;

        struct PendingSpawn
        {
            public float time;
            public EnemyDefinition enemy;
            public float angle;
        }

        void Awake()
        {
            EnemyRegistry.Clear();

            _useWaves = PrototypeConfig.Active.waves;
            _useBoss = PrototypeConfig.Active.boss;

            _poolRoot = new GameObject("~EnemyPool").transform;
            _poolRoot.SetParent(transform, false);

            if (stage == null)
            {
                Debug.LogError("[EnemySpawner] No StageDefinition assigned — nothing will spawn.", this);
                return;
            }

            foreach (EnemyDefinition def in PooledEnemies())
            {
                // Skip the boss prefab's pool entirely when bosses are off, rather
                // than pre-warming a 2.2x-scale prefab that will never spawn.
                if (!_useBoss && def == stage.boss) continue;

                if (def == null || def.prefab == null)
                {
                    Debug.LogError($"[EnemySpawner] Definition '{(def == null ? "null" : def.id)}' has no prefab.", this);
                    continue;
                }
                if (_pools.ContainsKey(def)) continue;

                // One boss exists per stage; pre-warming a couple of dozen of a
                // 2.2x-scale prefab is pure memory for nothing.
                int prewarm = def == stage.boss ? 1 : prewarmPerType;
                _pools[def] = new Pool<Enemy>(def.prefab, _poolRoot, prewarm);
            }
        }

        /// <summary>
        /// Every definition that might spawn, from whichever source is driving
        /// this run. Pooling only what the stage lists would leave the endless
        /// ramp's tiers to Instantiate mid-run, which is the one thing the pool
        /// exists to prevent.
        /// </summary>
        IEnumerable<EnemyDefinition> PooledEnemies()
        {
            var seen = new HashSet<EnemyDefinition>();

            foreach (EnemyDefinition def in stage.AllEnemies())
                if (def != null && seen.Add(def)) yield return def;

            if (ramp == null) yield break;

            foreach (EnemyDefinition def in ramp.AllEnemies())
                if (def != null && seen.Add(def)) yield return def;
        }

        void OnDestroy() => EnemyRegistry.Clear();

        void Start()
        {
            if (stage == null) return;

            if (_useWaves) BeginNextWave();
            else _phase = Phase.Waves;   // endless: the same phase, a different tick
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _elapsed += dt;

            switch (_phase)
            {
                case Phase.Waves:
                    if (_useWaves) TickWaves(dt);
                    else TickEndless(dt);
                    break;
                case Phase.BossIntro: TickBossIntro(dt); break;
                case Phase.Boss: TickBoss(); break;
            }
        }

        /// <summary>
        /// Prototype spawning, driven entirely by the SpawnRamp: the mix, the
        /// interval and the burst size all come from the wave number.
        /// </summary>
        void TickEndless(float dt)
        {
            _endlessTimer -= dt;
            if (_endlessTimer > 0f) return;

            int wave = EndlessWave;

            if (wave != _lastAnnouncedWave)
            {
                _lastAnnouncedWave = wave;
                WaveStarted?.Invoke(wave - 1, 0);
            }

            if (ramp == null)
            {
                _endlessTimer = 1f;
                return;
            }

            _endlessTimer = ramp.IntervalFor(wave);

            // A burst arrives together, from angles spread around the ring rather
            // than one point — the pressure should come from everywhere, which is
            // the whole premise of a fixed emplacement.
            int burst = ramp.BurstFor(wave);
            float baseAngle = UnityEngine.Random.value * 360f;

            for (int i = 0; i < burst; i++)
            {
                if (EnemyRegistry.Count >= maxConcurrent) break;

                EnemyDefinition def = ramp.Pick(wave);
                if (def == null) break;

                Spawn(def, baseAngle + 360f * i / burst, waveOverride: 0);
            }
        }

        /// <summary>
        /// The difficulty level. Driven by the player's level — which rises on
        /// kills — rather than by elapsed time.
        ///
        /// Tying it to kills makes the ramp self-balancing: a player clearing fast
        /// earns harder waves, and one who is struggling is not buried by a clock
        /// that does not care how they are doing. It also collapses two competing
        /// notions of progress into the single number the HUD shows.
        /// </summary>
        int EndlessWave => levelSource != null
            ? levelSource.Level
            : Mathf.FloorToInt(_elapsed / Mathf.Max(1f, secondsPerWave)) + 1;

        // ------------------------------------------------------------- waves

        void BeginNextWave()
        {
            _waveIndex++;

            if (_waveIndex >= stage.WaveCount)
            {
                BeginBossPhase();
                return;
            }

            WaveDefinition wave = stage.waves[_waveIndex];
            if (wave == null)
            {
                BeginNextWave();
                return;
            }

            BuildSchedule(wave);
            _waveElapsed = 0f;
            _cursor = 0;
            _phase = Phase.Waves;

            WaveStarted?.Invoke(_waveIndex, stage.WaveCount);
        }

        /// <summary>
        /// Flattens a wave into a sorted list of individual spawn instants.
        /// Done once per wave rather than evaluated every frame, so a wave with
        /// forty entries costs the same per frame as one with two.
        /// </summary>
        void BuildSchedule(WaveDefinition wave)
        {
            _pending.Clear();
            if (wave.entries == null) return;

            foreach (WaveEntry entry in wave.entries)
            {
                if (entry.enemy == null || entry.count <= 0) continue;

                float arcWidth = entry.arcDegrees > 0f ? entry.arcDegrees : 90f;
                float arcCentre = UnityEngine.Random.value * 360f;

                for (int i = 0; i < entry.count; i++)
                {
                    // Single-member groups sit at the start of their window, not
                    // the middle: a lone Brute should arrive when the designer
                    // said, not half a spread later.
                    float t = entry.count == 1 ? 0f : i / (float)(entry.count - 1);

                    _pending.Add(new PendingSpawn
                    {
                        time = entry.timeOffset + entry.spreadSeconds * t,
                        enemy = entry.enemy,
                        angle = AngleFor(entry.pattern, i, entry.count, arcCentre, arcWidth)
                    });
                }
            }

            _pending.Sort((a, b) => a.time.CompareTo(b.time));
        }

        static float AngleFor(SpawnPattern pattern, int index, int count, float arcCentre, float arcWidth)
        {
            switch (pattern)
            {
                case SpawnPattern.Ring:
                    // Offset the whole ring randomly so successive waves do not
                    // arrive on identical spokes.
                    return arcCentre + index / (float)count * 360f;

                case SpawnPattern.Arc:
                    float t = count == 1 ? 0.5f : index / (float)(count - 1);
                    return arcCentre + Mathf.Lerp(-arcWidth * 0.5f, arcWidth * 0.5f, t);

                case SpawnPattern.Point:
                    return arcCentre;

                default:
                    return UnityEngine.Random.value * 360f;
            }
        }

        void TickWaves(float dt)
        {
            _waveElapsed += dt;

            while (_cursor < _pending.Count && _pending[_cursor].time <= _waveElapsed)
            {
                // At the cap, hold the queue rather than dropping spawns. The wave
                // arrives late instead of arriving wrong, and the schedule catches
                // up as the field clears.
                if (EnemyRegistry.Count >= maxConcurrent) break;

                PendingSpawn next = _pending[_cursor];
                Spawn(next.enemy, next.angle);
                _cursor++;
            }

            bool everythingSpawned = _cursor >= _pending.Count;
            bool timeUp = _waveElapsed >= stage.waves[_waveIndex].duration;

            if (everythingSpawned && timeUp) BeginNextWave();
        }

        // -------------------------------------------------------------- boss

        void BeginBossPhase()
        {
            if (stage.boss == null || !_useBoss)
            {
                // No boss: the stage is beaten once the field is clear, so the
                // player is never left standing in an empty arena wondering.
                _phase = Phase.Boss;
                _boss = null;
                return;
            }

            _phase = Phase.BossIntro;
            _phaseTimer = stage.bossIntroSeconds;
        }

        void TickBossIntro(float dt)
        {
            _phaseTimer -= dt;
            if (_phaseTimer > 0f) return;

            // Wave 0, not the final wave index: a boss authored at 180 HP would
            // otherwise arrive with 1.12^8 applied and land near 450, making the
            // number in the asset meaningless to whoever is tuning it.
            _boss = Spawn(stage.boss, UnityEngine.Random.value * 360f, waveOverride: 0);
            _phase = Phase.Boss;

            if (_boss != null) BossSpawned?.Invoke(_boss);
        }

        void TickBoss()
        {
            if (_boss != null && !_boss.IsDead) return;

            // With a boss: its death ends the stage, stragglers or not — chasing
            // the last shambler around after the boss dies is anticlimax.
            // Without one: wait for the field to empty.
            if (_boss == null && EnemyRegistry.Count > 0) return;

            _phase = Phase.Complete;
            StageCleared?.Invoke();
        }

        // ------------------------------------------------------------ spawning

        Enemy Spawn(EnemyDefinition def, float angleDegrees, int waveOverride = -1)
        {
            if (def == null || target == null) return null;
            if (!_pools.TryGetValue(def, out Pool<Enemy> pool)) return null;

            float radians = angleDegrees * Mathf.Deg2Rad;
            Vector3 offset = new(Mathf.Cos(radians) * spawnRadius, 0f, Mathf.Sin(radians) * spawnRadius);

            Enemy enemy = pool.Get(target.position + offset, Quaternion.identity);

            // Wired once per object, not per spawn: a per-spawn lambda would
            // allocate on every zombie, and re-subscribing without unsubscribing
            // would return it to the pool once per life it had ever lived.
            if (_wired.Add(enemy)) enemy.Despawned += ReturnToPool;

            int wave = waveOverride >= 0 ? waveOverride : Mathf.Max(0, _waveIndex);
            enemy.Spawn(def, wave, stage.hpMultiplier, stage.damageMultiplier, target);
            return enemy;
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
