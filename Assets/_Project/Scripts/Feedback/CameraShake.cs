using UnityEngine;
using ScalePunch.Core;
using ScalePunch.Data;

namespace ScalePunch.Feedback
{
    /// <summary>
    /// Positional shake applied as a local offset, so it composes with
    /// CameraFollow instead of fighting it. Runs on unscaled time so the shake
    /// still plays during hitstop — that overlap is most of the punch.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraShake : MonoSingleton<CameraShake>
    {
        [SerializeField] CombatTuning tuning;

        Transform _transform;
        float _amplitude;
        float _remaining;
        float _duration;
        Vector3 _seed;

        protected override void Awake()
        {
            base.Awake();
            _transform = transform;
            _seed = new Vector3(Random.value * 100f, Random.value * 100f, Random.value * 100f);
        }

        public void Shake(float amplitude, float duration = -1f)
        {
            if (duration < 0f) duration = tuning != null ? tuning.shakeDuration : 0.18f;

            // Take the stronger of the two rather than summing; simultaneous
            // hits should not launch the camera off screen.
            if (amplitude >= _amplitude || _remaining <= 0f)
            {
                _amplitude = amplitude;
                _duration = duration;
                _remaining = duration;
            }
        }

        void LateUpdate()
        {
            if (_remaining <= 0f)
            {
                _transform.localPosition = Vector3.zero;
                return;
            }

            _remaining -= Time.unscaledDeltaTime;
            float falloff = _duration <= 0f ? 0f : Mathf.Clamp01(_remaining / _duration);
            float strength = _amplitude * falloff * falloff;
            float t = Time.unscaledTime * 28f;

            _transform.localPosition = new Vector3(
                (Mathf.PerlinNoise(_seed.x + t, 0f) - 0.5f) * 2f * strength,
                (Mathf.PerlinNoise(_seed.y + t, 0f) - 0.5f) * 2f * strength,
                0f);
        }
    }
}
