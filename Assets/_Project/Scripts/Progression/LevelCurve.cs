using UnityEngine;

namespace ScalePunch.Progression
{
    /// <summary>
    /// XP required per level. Quadratic, so early levels arrive fast and late
    /// ones stretch — the player must feel a draft within the first 25 seconds
    /// (docs/01-game-design.md §10) or the run has no hook.
    ///
    /// Create via Assets ▸ Create ▸ ScalePunch ▸ Level Curve.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Level Curve", fileName = "LevelCurve")]
    public class LevelCurve : ScriptableObject
    {
        [Header("cost(n) = flat + linear*n + quadratic*n²")]
        public float flat = 5f;
        public float linear = 8f;
        public float quadratic = 0.5f;

        [Tooltip("Levels beyond this cost the same as this one, so a long run " +
                 "does not stall completely.")]
        public int softCapLevel = 40;

        /// <summary>XP needed to go from <paramref name="level"/> to the next.</summary>
        public int CostFor(int level)
        {
            int n = Mathf.Min(Mathf.Max(1, level), softCapLevel);
            return Mathf.Max(1, Mathf.RoundToInt(flat + linear * n + quadratic * n * n));
        }
    }
}
