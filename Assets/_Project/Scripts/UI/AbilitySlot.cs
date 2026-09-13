using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ScalePunch.Abilities;

namespace ScalePunch.UI
{
    /// <summary>
    /// One booster slot: an active ability the player holds, with its cooldown
    /// filling a radial sweep.
    ///
    /// This is the only place an auto-firing active is visible at all. Without
    /// it the player drafts Shockwave and then has no idea they own it, when it
    /// fires, or whether levelling it changed anything — the ability may as well
    /// be a passive with a particle effect.
    /// </summary>
    public class AbilitySlot : MonoBehaviour
    {
        [SerializeField] Image frame;
        [SerializeField] Image icon;
        [Tooltip("Image Type = Filled, Radial 360, Origin Top, Clockwise.")]
        [SerializeField] Image cooldownFill;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] GameObject lockedOverlay;
        [SerializeField] TMP_Text lockedLabel;

        [Header("Colours")]
        [SerializeField] Color emptyFrame = new(0.18f, 0.16f, 0.26f);
        [SerializeField] Color filledFrame = new(0.42f, 0.34f, 0.72f);
        [Tooltip("Flashed for one frame's worth of time when the ability fires.")]
        [SerializeField] Color readyFlash = new(1f, 0.95f, 0.55f);
        [SerializeField] float flashSeconds = 0.18f;

        AbilityInstance _instance;
        float _lastProgress;
        float _flashRemaining;

        public bool IsEmpty => _instance == null;

        /// <summary>An unlocked but unfilled slot.</summary>
        public void SetEmpty()
        {
            _instance = null;

            if (icon != null) icon.enabled = false;
            if (cooldownFill != null) cooldownFill.fillAmount = 0f;
            if (levelLabel != null) levelLabel.text = string.Empty;
            if (frame != null) frame.color = emptyFrame;
            if (lockedOverlay != null) lockedOverlay.SetActive(false);
        }

        /// <summary>A slot the player has not earned yet.</summary>
        public void SetLocked(string reason)
        {
            _instance = null;

            if (icon != null) icon.enabled = false;
            if (cooldownFill != null) cooldownFill.fillAmount = 0f;
            if (levelLabel != null) levelLabel.text = string.Empty;
            if (frame != null) frame.color = emptyFrame;

            if (lockedOverlay != null) lockedOverlay.SetActive(true);
            if (lockedLabel != null) lockedLabel.text = reason;
        }

        public void Bind(AbilityInstance instance)
        {
            _instance = instance;
            _lastProgress = instance.CooldownProgress;

            if (lockedOverlay != null) lockedOverlay.SetActive(false);
            if (frame != null) frame.color = filledFrame;

            if (icon != null)
            {
                icon.sprite = instance.Definition.icon;
                // No icon art yet — a coloured block still says "this slot is
                // occupied", which is most of the information.
                icon.enabled = true;
                icon.color = instance.Definition.icon != null
                    ? Color.white
                    : new Color(0.75f, 0.80f, 0.95f);
            }

            if (levelLabel != null) levelLabel.text = $"L{instance.Level}";
        }

        public void Tick(float unscaledDelta)
        {
            if (_flashRemaining > 0f)
            {
                _flashRemaining -= unscaledDelta;
                if (_flashRemaining <= 0f && frame != null)
                    frame.color = _instance != null ? filledFrame : emptyFrame;
            }

            if (_instance == null || cooldownFill == null) return;

            float progress = _instance.CooldownProgress;

            // The cooldown resets the instant it fires, so progress falling is
            // the only signal that the ability went off. Catching it here is what
            // gives an automatic ability a visible moment.
            if (progress < _lastProgress - 0.1f) Flash();
            _lastProgress = progress;

            cooldownFill.fillAmount = progress;
        }

        void Flash()
        {
            _flashRemaining = flashSeconds;
            if (frame != null) frame.color = readyFlash;
        }
    }
}
