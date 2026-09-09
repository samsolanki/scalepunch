using UnityEngine;
using ScalePunch.Player;

namespace ScalePunch.Feedback
{
    /// <summary>
    /// A brief flare and light burst at the barrel on every shot.
    ///
    /// This is most of what makes automatic fire read as a gun rather than as
    /// pellets appearing from nowhere: the player's eye is on the muzzle, and a
    /// flash there sells the shot before the tracer has moved a metre.
    /// </summary>
    public class MuzzleFlash : MonoBehaviour
    {
        [SerializeField] AutoShoot weapon;
        [SerializeField] Renderer flare;
        [SerializeField] Light burst;

        [Header("Timing")]
        [Tooltip("Seconds visible. Two or three frames - longer reads as a lamp.")]
        [SerializeField] float duration = 0.045f;
        [SerializeField] float lightIntensity = 4.5f;

        [Header("Variation")]
        [Tooltip("Random scale spread. Identical flashes at 3 shots a second " +
                 "read as a strobe; a little variety reads as firing.")]
        [SerializeField] float scaleJitter = 0.25f;
        [SerializeField] float baseScale = 0.22f;

        float _remaining;

        void Awake() => SetVisible(false);

        void OnEnable()
        {
            if (weapon != null) weapon.Fired += OnFired;
        }

        void OnDisable()
        {
            if (weapon != null) weapon.Fired -= OnFired;
        }

        void OnFired()
        {
            _remaining = duration;

            float scale = baseScale * (1f + Random.Range(-scaleJitter, scaleJitter));
            transform.localScale = Vector3.one * scale;
            transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            SetVisible(true);
            if (burst != null) burst.intensity = lightIntensity;
        }

        void Update()
        {
            if (_remaining <= 0f) return;

            // Unscaled: the flash must still play during the hitstop its own
            // bullet causes, which is the frame the player is looking at.
            _remaining -= Time.unscaledDeltaTime;

            if (burst != null && duration > 0f)
                burst.intensity = lightIntensity * Mathf.Clamp01(_remaining / duration);

            if (_remaining <= 0f) SetVisible(false);
        }

        void SetVisible(bool visible)
        {
            if (flare != null) flare.enabled = visible;
            if (burst != null) burst.enabled = visible;
        }
    }
}
