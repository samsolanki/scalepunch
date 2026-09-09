using UnityEngine;
using ScalePunch.Data;

namespace ScalePunch.Core
{
    /// <summary>
    /// The single owner of Time.timeScale. Nothing else may write it.
    ///
    /// Two things want to slow time and they must not fight: impact hitstop
    /// (brief, frequent) and hard pauses (the level-up draft, menus). When these
    /// were separate systems, a draft opening during a hitstop would have the
    /// hitstop expire a frame later and set timeScale back to 1 — unpausing the
    /// game underneath the open draft screen. Pauses always win here.
    /// </summary>
    public class TimeController : MonoSingleton<TimeController>
    {
        [SerializeField] CombatTuning tuning;

        int _pauseDepth;
        float _freezeRemaining;

        public bool IsPaused => _pauseDepth > 0;
        public bool IsFrozen => _freezeRemaining > 0f;

        /// <summary>Hard pause. Reference-counted, so nested menus behave.</summary>
        public void PushPause()
        {
            _pauseDepth++;
            Apply();
        }

        public void PopPause()
        {
            _pauseDepth = Mathf.Max(0, _pauseDepth - 1);
            Apply();
        }

        /// <summary>
        /// Impact hitstop. Requests do not stack — the longest outstanding freeze
        /// wins, so a 40-zombie burst reads as one solid thump rather than a
        /// two-second lockup.
        /// </summary>
        public void Freeze(float duration)
        {
            if (duration <= 0f || IsPaused) return;

            _freezeRemaining = Mathf.Max(_freezeRemaining, duration);
            Apply();
        }

        void Update()
        {
            if (_freezeRemaining <= 0f) return;

            _freezeRemaining -= Time.unscaledDeltaTime;
            if (_freezeRemaining <= 0f)
            {
                _freezeRemaining = 0f;
                Apply();
            }
        }

        void Apply()
        {
            if (_pauseDepth > 0) { Time.timeScale = 0f; return; }

            Time.timeScale = _freezeRemaining > 0f
                ? (tuning != null ? tuning.hitstopTimeScale : 0.05f)
                : 1f;
        }

        protected override void OnDestroy()
        {
            // A scene unload mid-pause would otherwise leave the next scene frozen.
            Time.timeScale = 1f;
            base.OnDestroy();
        }
    }
}
