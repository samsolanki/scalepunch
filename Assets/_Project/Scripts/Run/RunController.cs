using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using ScalePunch.Combat;
using ScalePunch.Core;
using ScalePunch.Enemies;
using ScalePunch.Progression;
using ScalePunch.Stages;

namespace ScalePunch.Run
{
    /// <summary>
    /// Owns whether the run is still going, and ends it.
    ///
    /// Before this existed a run had no beginning, no end and no consequence:
    /// the player died and the zombies kept walking. Everything downstream —
    /// currencies, gear, the meta layer — hangs off a run being able to finish.
    /// </summary>
    public class RunController : MonoBehaviour
    {
        [SerializeField] StageDefinition stage;
        [SerializeField] Health playerHealth;
        [SerializeField] EnemySpawner spawner;
        [SerializeField] LevelSystem levels;

        [Tooltip("Seconds between the run ending and the screen appearing. The beat " +
                 "lets the killing blow land before the UI covers it.")]
        [SerializeField] float endDelaySeconds = 0.9f;

        public event Action<RunResult> RunEnded;
        /// <summary>Wave index and total, forwarded from the spawner for the HUD.</summary>
        public event Action<int, int> WaveStarted;
        public event Action<Enemy> BossSpawned;

        public RunOutcome Outcome { get; private set; } = RunOutcome.InProgress;
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public bool IsOver => Outcome != RunOutcome.InProgress;

        float _endTimer;
        bool _ending;
        bool _paused;

        void OnEnable()
        {
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
            if (spawner != null)
            {
                spawner.StageCleared += OnStageCleared;
                spawner.WaveStarted += OnWaveStarted;
                spawner.BossSpawned += OnBossSpawned;
            }
            EnemyRegistry.Killed += OnKill;
        }

        void OnDisable()
        {
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
            if (spawner != null)
            {
                spawner.StageCleared -= OnStageCleared;
                spawner.WaveStarted -= OnWaveStarted;
                spawner.BossSpawned -= OnBossSpawned;
            }
            EnemyRegistry.Killed -= OnKill;
        }

        void Update()
        {
            if (!IsOver)
            {
                Elapsed += Time.deltaTime;
                return;
            }

            if (!_ending) return;

            // Unscaled: the killing blow's hitstop must not stretch this beat.
            _endTimer -= Time.unscaledDeltaTime;
            if (_endTimer > 0f) return;

            _ending = false;
            Freeze();
            RunEnded?.Invoke(BuildResult());
        }

        void OnKill(Enemy enemy) => Kills++;
        void OnWaveStarted(int index, int total) => WaveStarted?.Invoke(index, total);
        void OnBossSpawned(Enemy boss) => BossSpawned?.Invoke(boss);

        void OnPlayerDied(DamageInfo info) => End(RunOutcome.Defeat);
        void OnStageCleared() => End(RunOutcome.Victory);

        void End(RunOutcome outcome)
        {
            if (IsOver) return;   // a boss dying on the same frame the player does

            Outcome = outcome;
            _ending = true;
            _endTimer = endDelaySeconds;
        }

        void Freeze()
        {
            if (_paused || !TimeController.Exists) return;

            _paused = true;
            TimeController.Instance.PushPause();
        }

        RunResult BuildResult()
        {
            int waves = spawner != null ? spawner.CurrentWaveNumber : 0;
            int total = spawner != null ? spawner.TotalWaves : 0;
            int level = levels != null ? levels.Level : 1;

            return new RunResult(Outcome, Elapsed, Mathf.Min(waves, total), total,
                                 Kills, level, Payout());
        }

        /// <summary>
        /// A losing run still pays. Four minutes of effort rewarded with nothing
        /// is how you lose a player permanently (docs/01-game-design.md §6).
        ///
        /// P2 hands this to a CurrencyService; for now it is computed and shown
        /// so the number can be balanced before anything banks it.
        /// </summary>
        int Payout()
        {
            if (stage == null) return 0;

            float clearBonus = Outcome == RunOutcome.Victory
                ? stage.coinsOnClear
                : stage.coinsOnClear * stage.failPayoutFraction;

            return Mathf.RoundToInt(clearBonus + Kills * stage.coinsPerKill);
        }

        public void Restart()
        {
            // Must come before the load: a restart pressed from the paused end
            // screen would otherwise bring up the next scene at timeScale zero.
            if (TimeController.Exists) TimeController.Instance.ResetAll();
            else Time.timeScale = 1f;

            UnityEngine.SceneManagement.Scene scene = gameObject.scene;

            // buildIndex is -1 for a scene that was never added to Build Settings,
            // and LoadScene(-1) throws. Name works either way once it is listed;
            // if it is not, say so rather than dying silently on the retry button.
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else if (Application.CanStreamedLevelBeLoaded(scene.name)) SceneManager.LoadScene(scene.name);
            else Debug.LogError($"[RunController] Scene '{scene.name}' is not in Build Settings — " +
                                "cannot restart. Add it via File ▸ Build Settings.", this);
        }
    }
}
