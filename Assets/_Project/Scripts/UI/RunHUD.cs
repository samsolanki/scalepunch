using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ScalePunch.Combat;
using ScalePunch.Enemies;
using ScalePunch.Player;
using ScalePunch.Progression;

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

        [Header("XP")]
        [SerializeField] Image xpFill;
        [SerializeField] TMP_Text levelLabel;

        [Header("Health")]
        [SerializeField] Image healthFill;
        [SerializeField] TMP_Text healthLabel;

        [Header("Run")]
        [SerializeField] TMP_Text timerLabel;
        [SerializeField] TMP_Text killsLabel;

        float _elapsed;
        int _kills;

        void OnEnable()
        {
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
            EnemyRegistry.Killed += OnKill;
        }

        void OnDisable()
        {
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
            EnemyRegistry.Killed -= OnKill;
        }

        void OnPlayerDied(DamageInfo info) => enabled = false;
        void OnKill(Enemy enemy) => _kills++;

        void Update()
        {
            // Scaled, so the run clock stops during a draft and during hitstop.
            _elapsed += Time.deltaTime;

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
