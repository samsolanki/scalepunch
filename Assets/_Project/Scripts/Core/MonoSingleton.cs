using UnityEngine;

namespace ScalePunch.Core
{
    /// <summary>Scene-scoped singleton. Deliberately not DontDestroyOnLoad —
    /// run-scoped services must die with the Run scene.</summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<T>();
                return _instance;
            }
        }

        public static bool Exists => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
