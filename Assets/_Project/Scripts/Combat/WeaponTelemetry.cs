using UnityEngine;

namespace ScalePunch.Combat
{
    /// <summary>
    /// Counts rounds fired against rounds that hit something.
    ///
    /// Exists because "it feels like it misses" is not a number, and this bug
    /// survived two rounds of fixes that each sounded right. Accuracy is the
    /// only way to tell a real regression from a run of bad luck, and it costs
    /// two integers.
    ///
    /// Editor and development builds only.
    /// </summary>
    public static class WeaponTelemetry
    {
        public static int Fired { get; private set; }
        public static int Hit { get; private set; }
        public static int Expired { get; private set; }

        public static float Accuracy => Fired == 0 ? 0f : Hit / (float)Fired;

        [Tooltip("Rounds between accuracy reports.")]
        const int ReportEvery = 50;

        public static void Reset()
        {
            Fired = 0;
            Hit = 0;
            Expired = 0;
        }

        public static void ReportFired()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Fired++;
            if (Fired % ReportEvery == 0) Log();
#endif
        }

        public static void ReportHit()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Hit++;
#endif
        }

        public static void ReportExpired()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Expired++;
#endif
        }

        static void Log()
            => Debug.Log($"[Weapon] {Hit}/{Fired} rounds connected ({Accuracy * 100f:0}%), " +
                         $"{Expired} expired short. Anything under ~85% against walkers " +
                         "means aiming is wrong, not unlucky.");
    }
}
