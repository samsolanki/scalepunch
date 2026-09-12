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
        /// <summary>Rounds that expired with a live target still out there — a
        /// genuine aiming failure.</summary>
        public static int Expired { get; private set; }
        /// <summary>Rounds whose target died before they landed. Wasted, but not
        /// a miss: counting these as misses hides real regressions in noise.</summary>
        public static int Wasted { get; private set; }

        public static float Accuracy
        {
            get
            {
                int meaningful = Fired - Wasted;
                return meaningful <= 0 ? 1f : Hit / (float)meaningful;
            }
        }

        [Tooltip("Rounds between accuracy reports.")]
        const int ReportEvery = 50;

        public static void Reset()
        {
            Fired = 0;
            Hit = 0;
            Expired = 0;
            Wasted = 0;
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

        public static void ReportWasted()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Wasted++;
#endif
        }

        static void Log()
            => Debug.Log($"[Weapon] {Hit}/{Fired - Wasted} rounds connected ({Accuracy * 100f:0}%). " +
                         $"{Expired} missed, {Wasted} spent on a zombie that died first.\n" +
                         "With guaranteedHit on, 'missed' should be 0. Anything above that " +
                         "is a real bug, not variance.");
    }
}
