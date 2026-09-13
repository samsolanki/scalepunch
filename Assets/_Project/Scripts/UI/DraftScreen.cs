using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Abilities;
using ScalePunch.Player;

namespace ScalePunch.UI
{
    /// <summary>
    /// Shows the three cards while the game is paused. Owns no draft logic —
    /// it renders whatever DraftController offers and reports the click back.
    /// </summary>
    public class DraftScreen : MonoBehaviour
    {
        [SerializeField] DraftController draft;
        [SerializeField] PlayerStats stats;
        [SerializeField] GameObject panel;
        [SerializeField] DraftCard[] cards;
        [SerializeField] CanvasGroup canvasGroup;

        [Header("Entrance")]
        [Tooltip("Seconds to fade in. Runs on unscaled time — the game is paused.")]
        [SerializeField] float fadeSeconds = 0.15f;

        float _fadeElapsed;
        bool _fading;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        void OnEnable()
        {
            if (draft != null) draft.OfferReady += Show;
        }

        void OnDisable()
        {
            if (draft != null) draft.OfferReady -= Show;
        }

        void Show(IReadOnlyList<AbilityOffer> offer)
        {
            if (panel != null) panel.SetActive(true);

            for (int i = 0; i < cards.Length; i++)
            {
                bool hasCard = i < offer.Count;
                cards[i].gameObject.SetActive(hasCard);
                if (!hasCard) continue;

                cards[i].Bind(offer[i], stats.Stats, Choose);
            }

            _fadeElapsed = 0f;
            _fading = fadeSeconds > 0f && canvasGroup != null;
            if (canvasGroup != null) canvasGroup.alpha = _fading ? 0f : 1f;
        }

        void Update()
        {
            if (!_fading) return;

            // Unscaled: Time.deltaTime is zero while the draft holds the pause.
            _fadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_fadeElapsed / fadeSeconds);

            canvasGroup.alpha = t;
            if (t >= 1f) _fading = false;
        }

        void Choose(AbilityOffer offer)
        {
            // Hidden before Choose so a queued second level-up re-shows the panel
            // with fresh cards rather than leaving the old ones on screen.
            if (panel != null) panel.SetActive(false);
            draft.Choose(offer);
        }
    }
}
