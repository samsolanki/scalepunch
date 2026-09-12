using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ScalePunch.Combat;
using ScalePunch.Core;
using ScalePunch.Enemies;
using ScalePunch.Player;
using ScalePunch.Progression;
using ScalePunch.Run;

namespace ScalePunch.UI
{
    /// <summary>
    /// In-run readouts: health, level progress, kills, timer.
    ///
    /// Level progress is the most important element on screen — it is the only
    /// promise the run makes — so it sits on the bottom bar next to health,
    /// where a thumb is already looking, rather than at the top edge where it
    /// competes with the kill counter.
    /// </summary>
    public class RunHUD : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] LevelSystem levels;
        [SerializeField] Health playerHealth;
        [SerializeField] AutoShoot weapon;
        [SerializeField] RunController run;

        [Header("Health")]
        [SerializeField] Image healthFill;
        [SerializeField] TMP_Text healthLabel;
        [Tooltip("Trails the real value so a big hit reads as a chunk lost rather " +
                 "than the bar teleporting.")]
        [SerializeField] Image healthDelayedFill;
        [SerializeField] float delayedDrainPerSecond = 0.55f;

        [Header("Health colours")]
        [SerializeField] Color healthyColour = new(0.30f, 0.82f, 0.22f);
        [SerializeField] Color hurtColour = new(0.95f, 0.75f, 0.15f);
        [SerializeField] Color criticalColour = new(0.90f, 0.22f, 0.22f);
        [Range(0f, 1f)] [SerializeField] float hurtBelow = 0.55f;
        [Range(0f, 1f)] [SerializeField] float criticalBelow = 0.25f;

        [Header("Level track")]
        [Tooltip("Kills toward the next level. One progression drives the draft AND " +
                 "the spawn ramp, so this single bar is the run's whole shape.")]
        [SerializeField] Image xpFill;
        [SerializeField] TMP_Text levelLabel;
        [Tooltip("The fraction, e.g. \"3 / 5\". Without it the bar is a vibe, not " +
                 "a target the player can count down.")]
        [SerializeField] TMP_Text xpLabel;

        [Header("Run")]
        [SerializeField] TMP_Text timerLabel;
        [Tooltip("Total kills this run — a running tally, not progress.")]
        [SerializeField] TMP_Text killsLabel;
        [SerializeField] TMP_Text waveLabel;

        [Header("Boss")]
        [SerializeField] GameObject bossBanner;
        [SerializeField] float bossBannerSeconds = 2.5f;

        float _elapsed;
        int _kills;
        float _bannerRemaining;
        float _delayed = 1f;

        void Awake()
        {
            if (bossBanner != null) bossBanner.SetActive(false);

            // No waves means no wave counter — a static "WAVE 1/8" that never
            // moves is worse than no label at all.
            if (waveLabel != null) waveLabel.gameObject.SetActive(PrototypeConfig.Active.waves);
        }

        void OnEnable()
        {
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
            EnemyRegistry.Killed += OnKill;

            if (run != null)
            {
                run.WaveStarted += OnWaveStarted;
                run.BossSpawned += OnBossSpawned;
            }
        }

        void OnDisable()
        {
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
            EnemyRegistry.Killed -= OnKill;

            if (run != null)
            {
                run.WaveStarted -= OnWaveStarted;
                run.BossSpawned -= OnBossSpawned;
            }
        }

        void OnPlayerDied(DamageInfo info) => enabled = false;
        void OnKill(Enemy enemy) => _kills++;

        void OnWaveStarted(int index, int total)
        {
            // "Level", not "Wave": the spawn ramp and the draft advance on the
            // same number now, and two names for one thing is one too many.
            if (waveLabel != null) waveLabel.text = $"LEVEL {index + 1}/{total}";
        }

        void OnBossSpawned(Enemy boss)
        {
            if (waveLabel != null) waveLabel.text = "BOSS";
            if (bossBanner == null) return;

            bossBanner.SetActive(true);
            _bannerRemaining = bossBannerSeconds;
        }

        void Update()
        {
            // Scaled, so the run clock stops during a draft and during hitstop.
            _elapsed += Time.deltaTime;

            if (_bannerRemaining > 0f)
            {
                // Unscaled: the banner must still time out if it lands on a
                // hitstop frame.
                _bannerRemaining -= Time.unscaledDeltaTime;
                if (_bannerRemaining <= 0f && bossBanner != null) bossBanner.SetActive(false);
            }

            TickHealth();
            TickLevel();

            if (timerLabel != null)
                timerLabel.text = $"{Mathf.FloorToInt(_elapsed / 60f):0}:{Mathf.FloorToInt(_elapsed % 60f):00}";

            if (killsLabel != null) killsLabel.text = _kills.ToString();
        }

        void TickHealth()
        {
            if (playerHealth == null) return;

            float normalised = playerHealth.Normalised;

            if (healthFill != null)
            {
                healthFill.fillAmount = normalised;

                // Colour is the fastest read of "am I in trouble" — far faster
                // than parsing two numbers while a crowd closes in.
                healthFill.color = normalised <= criticalBelow ? criticalColour
                                 : normalised <= hurtBelow ? hurtColour
                                 : healthyColour;
            }

            if (healthDelayedFill != null)
            {
                _delayed = _delayed < normalised
                    ? normalised
                    : Mathf.MoveTowards(_delayed, normalised, delayedDrainPerSecond * Time.unscaledDeltaTime);

                healthDelayedFill.fillAmount = _delayed;
            }

            if (healthLabel != null)
                healthLabel.text = $"{Mathf.CeilToInt(playerHealth.Current)} / {Mathf.CeilToInt(playerHealth.Max)}";
        }

        void TickLevel()
        {
            if (levels == null) return;

            if (xpFill != null) xpFill.fillAmount = levels.Progress;
            if (levelLabel != null) levelLabel.text = $"LV {levels.Level}";
            if (xpLabel != null) xpLabel.text = $"{levels.Current} / {levels.Required}";
        }
    }
}
