using UnityEngine;
using ScalePunch.Core;
using ScalePunch.Data;

namespace ScalePunch.Feedback
{
    /// <summary>
    /// Global time-scale freeze on impact. Requests do not stack — the longest
    /// outstanding freeze wins, so a 40-enemy AoE hit reads as one solid thump
    /// instead of a two-second lockup.
    /// </summary>
    public class Hitstop : MonoSingleton<Hitstop>
    {
        [SerializeField] CombatTuning tuning;

        float _remaining;
        float _restoreTimeScale = 1f;

        public void Freeze(float duration)
        {
            if (duration <= 0f) return;

            if (_remaining <= 0f)
            {
                _restoreTimeScale = Time.timeScale;
                Time.timeScale = tuning != null ? tuning.hitstopTimeScale : 0.05f;
            }
            _remaining = Mathf.Max(_remaining, duration);
        }

        void Update()
        {
            if (_remaining <= 0f) return;

            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                Time.timeScale = _restoreTimeScale;
            }
        }

        protected override void OnDestroy()
        {
            // A scene unload mid-freeze would otherwise leave the game in slow motion.
            if (_remaining > 0f) Time.timeScale = _restoreTimeScale;
            base.OnDestroy();
        }
    }
}
