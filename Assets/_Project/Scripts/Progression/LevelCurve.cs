using UnityEngine;

namespace ScalePunch.Progression
{
    public enum ProgressSource
    {
        /// <summary>Kills counted directly. A draft at "5 zombies" means exactly
        /// that, with no pickup delay and no per-enemy XP weighting.</summary>
        Kills,
        /// <summary>Collected XP gems. Lets a Brute be worth more than a
        /// Shambler, at the cost of the threshold no longer being a kill count.</summary>
        XPGems
    }

    /// <summary>
    /// How much progress each level costs.
    ///
    /// Authored as **cumulative totals** because that is how the pacing is
    /// actually reasoned about — "first draft at 5 kills, second at 15" — rather
    /// than as per-level increments you have to add up in your head.
    ///
    /// Create via Assets ▸ Create ▸ ScalePunch ▸ Level Curve.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Level Curve", fileName = "LevelCurve")]
    public class LevelCurve : ScriptableObject
    {
        [Tooltip("What counts as progress. Kills makes the thresholds below literal " +
                 "kill counts; XP Gems makes them collected gem value.")]
        public ProgressSource source = ProgressSource.Kills;

        [Header("Authored thresholds (cumulative)")]
        [Tooltip("Total progress needed to REACH each level, starting at level 2. " +
                 "{5, 15} = first draft at 5 kills, second at 15. Beyond this list " +
                 "the formula below takes over.")]
        public int[] cumulativeThresholds = { 5, 15, 30, 50, 75, 105, 140, 180 };

        [Header("Beyond the list: cost(n) = flat + linear*n + quadratic*n²")]
        public float flat = 5f;
        public float linear = 8f;
        public float quadratic = 0.5f;

        [Tooltip("Levels past this cost the same as this one, so a long run does " +
                 "not stall completely.")]
        public int softCapLevel = 40;

        /// <summary>Progress needed to go from <paramref name="level"/> to the next.</summary>
        public int CostFor(int level)
        {
            int from = Mathf.Max(1, level);
            return Mathf.Max(1, CumulativeFor(from + 1) - CumulativeFor(from));
        }

        /// <summary>Total progress needed to reach <paramref name="level"/>.
        /// Level 1 is the start, so it costs nothing.</summary>
        public int CumulativeFor(int level)
        {
            if (level <= 1) return 0;

            int index = level - 2;   // thresholds[0] is the cost of reaching level 2
            if (cumulativeThresholds != null && index < cumulativeThresholds.Length)
                return Mathf.Max(1, cumulativeThresholds[index]);

            // Past the authored list, extend from its last value with the formula
            // so there is no cliff where hand-authoring stops.
            int authoredCount = cumulativeThresholds?.Length ?? 0;
            int running = authoredCount > 0
                ? cumulativeThresholds[authoredCount - 1]
                : 0;

            for (int n = authoredCount + 2; n <= level; n++)
                running += FormulaCost(n - 1);

            return running;
        }

        int FormulaCost(int level)
        {
            int n = Mathf.Min(Mathf.Max(1, level), softCapLevel);
            return Mathf.Max(1, Mathf.RoundToInt(flat + linear * n + quadratic * n * n));
        }

        void OnValidate()
        {
            if (cumulativeThresholds == null) return;

            // Non-increasing thresholds mean a level that costs nothing, which
            // fires an endless chain of drafts on a single kill.
            for (int i = 1; i < cumulativeThresholds.Length; i++)
                if (cumulativeThresholds[i] <= cumulativeThresholds[i - 1])
                    cumulativeThresholds[i] = cumulativeThresholds[i - 1] + 1;
        }
    }
}
