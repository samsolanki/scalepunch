using UnityEngine;

namespace ScalePunch.Save
{
    /// <summary>
    /// Writes the profile when the app goes away.
    ///
    /// OnApplicationPause is the one that matters on mobile: Android and iOS can
    /// kill a backgrounded app without ever calling OnApplicationQuit, so a game
    /// that only saves on quit loses the session roughly whenever the OS feels
    /// like it.
    /// </summary>
    public class SaveHooks : MonoBehaviour
    {
        [Tooltip("Also save on a timer, as a backstop for a hard crash.")]
        [SerializeField] float autosaveSeconds = 60f;

        float _timer;

        void Awake()
        {
            // Touching Current forces the load early, so the first read is not a
            // file hit in the middle of gameplay.
            _ = SaveService.Current;
            _timer = autosaveSeconds;
        }

        void Update()
        {
            if (autosaveSeconds <= 0f) return;

            // Unscaled: a paused game should still autosave.
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;

            _timer = autosaveSeconds;
            SaveService.Save();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveService.Save();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) SaveService.Save();
        }

        void OnApplicationQuit() => SaveService.Save();
    }
}
