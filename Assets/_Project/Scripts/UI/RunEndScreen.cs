using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ScalePunch.Meta;
using ScalePunch.Run;

namespace ScalePunch.UI
{
    /// <summary>
    /// Victory / defeat summary. Renders a RunResult and offers a retry.
    ///
    /// Deliberately shows the same rows on both outcomes, with the coin line
    /// always present: a defeat screen that hides the payout teaches the player
    /// that losing is worthless, which is the opposite of what the economy wants.
    /// </summary>
    public class RunEndScreen : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] GameObject panel;
        [SerializeField] CanvasGroup canvasGroup;

        [Header("Labels")]
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text subtitleLabel;
        [SerializeField] TMP_Text statsLabel;
        [SerializeField] TMP_Text coinsLabel;
        [SerializeField] TMP_Text balanceLabel;

        [Header("Buttons")]
        [SerializeField] Button retryButton;

        [Header("Look")]
        [SerializeField] Color victoryColour = new(0.35f, 0.9f, 0.45f);
        [SerializeField] Color defeatColour = new(0.9f, 0.32f, 0.32f);
        [SerializeField] float fadeSeconds = 0.35f;

        float _fadeElapsed;
        bool _fading;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
        }

        void OnDestroy()
        {
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetry);
        }

        void OnEnable()
        {
            if (run != null) run.RunEnded += Show;
        }

        void OnDisable()
        {
            if (run != null) run.RunEnded -= Show;
        }

        void Show(RunResult result)
        {
            if (panel != null) panel.SetActive(true);

            if (titleLabel != null)
            {
                titleLabel.text = result.IsVictory ? "STAGE CLEAR" : "OVERRUN";
                titleLabel.color = result.IsVictory ? victoryColour : defeatColour;
            }

            if (subtitleLabel != null)
                subtitleLabel.text = result.IsVictory
                    ? "The ring held."
                    : $"You held {result.WavesSurvived} of {result.TotalWaves} waves.";

            if (statsLabel != null)
                statsLabel.text =
                    $"Time      {Mathf.FloorToInt(result.Duration / 60f)}:{Mathf.FloorToInt(result.Duration % 60f):00}\n" +
                    $"Kills     {result.Kills}\n" +
                    $"Level     {result.Level}\n" +
                    $"Waves     {result.WavesSurvived}/{result.TotalWaves}";

            if (coinsLabel != null) coinsLabel.text = $"+{result.Coins}";

            // The new total, beside the gain. A reward with no running balance
            // beside it does not read as progress toward anything.
            if (balanceLabel != null)
                balanceLabel.text = $"{CurrencyService.Format(result.CoinBalance)} total";

            _fadeElapsed = 0f;
            _fading = fadeSeconds > 0f && canvasGroup != null;
            if (canvasGroup != null) canvasGroup.alpha = _fading ? 0f : 1f;
        }

        void Update()
        {
            if (!_fading) return;

            // Unscaled: the run is paused behind this screen.
            _fadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_fadeElapsed / fadeSeconds);

            canvasGroup.alpha = t;
            if (t >= 1f) _fading = false;
        }

        void OnRetry()
        {
            if (run != null) run.Restart();
        }
    }
}
