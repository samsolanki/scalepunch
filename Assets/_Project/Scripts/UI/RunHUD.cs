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
    /// In-run readouts: XP, level, health, elapsed time, kills.
    ///
    /// The XP bar is the most important element on screen — it is the only
    /// promise the run makes, so it belongs at the top edge, full width, where
    /// a thumb cannot cover it.
    /// </summary>
    public class RunHUD : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] LevelSystem levels;
        [SerializeField] Health playerHealth;
        [SerializeField] AutoShoot weapon;
        [SerializeField] RunController run;

        [Header("XP")]
        [SerializeField] Image xpFill;
        [SerializeField] TMP_Text levelLabel;

        [Header("Health")]
        [SerializeField] Image healthFill;
        [SerializeField] TMP_Text healthLabel;

        [Header("Run")]
        [SerializeField] TMP_Text timerLabel;
        [SerializeField] TMP_Text killsLabel;
        [SerializeField] TMP_Text waveLabel;

        [Header("Boss")]
        [SerializeField] GameObject bossBanner;
        [SerializeField] float bossBannerSeconds = 2.5f;

        float _elapsed;
        int _kills;
        float _bannerRemaining;

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
            if (waveLabel != null) waveLabel.text = $"WAVE {index + 1}/{total}";
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

            if (levels != null)
            {
                if (xpFill != null) xpFill.fillAmount = levels.Progress;
                if (levelLabel != null) levelLabel.text = levels.Level.ToString();
            }

            if (playerHealth != null)
            {
                if (healthFill != null) healthFill.fillAmount = playerHealth.Normalised;
                if (healthLabel != null)
                    healthLabel.text = $"{Mathf.CeilToInt(playerHealth.Current)}/{Mathf.CeilToInt(playerHealth.Max)}";
            }

            if (timerLabel != null)
                timerLabel.text = $"{Mathf.FloorToInt(_elapsed / 60f):0}:{Mathf.FloorToInt(_elapsed % 60f):00}";

            if (killsLabel != null) killsLabel.text = _kills.ToString();
        }
    }
}
