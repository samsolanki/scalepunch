using UnityEngine;
using ScalePunch.Enemies;

namespace ScalePunch.Combat
{
    /// <summary>
    /// Reports what every round did.
    ///
    /// Two levels, because they answer different questions. The per-round lines
    /// answer "did THAT shot land", which is what you need while watching the
    /// game. The summary answers "is aiming broken", which no single shot can
    /// tell you.
    ///
    /// Editor and development builds only.
    /// </summary>
    public static class WeaponTelemetry
    {
        /// <summary>Per-round lines. Off for a real build; at 3 shots a second it
        /// is a lot of console.</summary>
        public static bool Verbose = true;

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

        const int ReportEvery = 50;
        static int _nextShotId;

        public static void Reset()
        {
            Fired = 0;
            Hit = 0;
            Expired = 0;
            Wasted = 0;
            _nextShotId = 0;
        }

        /// <summary>Returns the round's id so its later lines can be matched to it.</summary>
        public static int ReportFired(Enemy target, Vector3 from)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Fired++;
            int id = ++_nextShotId;

            if (Verbose)
            {
                string name = target != null && target.Definition != null ? target.Definition.id : "nothing";
                float distance = target != null ? Flat(target.transform.position - from).magnitude : 0f;

                Debug.Log($"<color=#9ad>▶ shot {id}</color>  FIRED at {name} at {distance:0.0} m");
            }

            if (Fired % ReportEvery == 0) LogSummary();
            return id;
#else
            return 0;
#endif
        }

        public static void ReportHit(int shotId, Enemy target, float damage, float hpBefore, float hpAfter)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Hit++;
            if (!Verbose) return;

            string name = target != null && target.Definition != null ? target.Definition.id : "?";
            string outcome = hpAfter <= 0f ? "KILLED" : "hit";

            Debug.Log($"<color=#6f6>✔ shot {shotId}</color>  {outcome} {name} for {damage:0.#} — " +
                      $"HP {hpBefore:0.#} → {hpAfter:0.#}");
#endif
        }

        public static void ReportWasted(int shotId)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Wasted++;
            if (Verbose)
                Debug.Log($"<color=#999>· shot {shotId}</color>  spent — its target died first");
#endif
        }

        /// <summary>
        /// A genuine miss, with the geometry that caused it.
        ///
        /// Whether a round died 0.1 m off a Brute's flank or 8 m from anything
        /// points at completely different bugs, and "it missed" distinguishes
        /// neither.
        /// </summary>
        public static void ReportExpired(int shotId, Vector3 where, Enemy target, float roundRadius)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Expired++;
            if (target == null) return;

            float distance = Flat(target.transform.position - where).magnitude;
            float gap = distance - (roundRadius + target.BodyRadius);
            string name = target.Definition != null ? target.Definition.id : "?";

            Debug.LogWarning($"<color=#f66>✘ shot {shotId}</color>  MISSED {name} — expired {distance:0.00} m away, " +
                             $"{gap:0.00} m outside its reach (round {roundRadius:0.00} + body {target.BodyRadius:0.00}). " +
                             $"{EnemyRegistry.Count} zombies live.");
#endif
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        static void LogSummary()
            => Debug.Log($"<color=#fc6>■ WEAPON</color>  {Hit}/{Fired - Wasted} connected ({Accuracy * 100f:0}%). " +
                         $"{Expired} missed, {Wasted} spent on a zombie that died first.\n" +
                         "With guaranteedHit on, missed should be 0.");
    }
}
