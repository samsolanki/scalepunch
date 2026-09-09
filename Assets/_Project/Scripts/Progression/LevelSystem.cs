using System;
using UnityEngine;

namespace ScalePunch.Progression
{
    /// <summary>
    /// Run-scoped XP and levelling. Owns nothing but numbers — the draft
    /// listens for LevelledUp and decides what to do about it.
    /// </summary>
    public class LevelSystem : MonoBehaviour
    {
        [SerializeField] LevelCurve curve;

        public int Level { get; private set; } = 1;
        public int CurrentXP { get; private set; }
        public int XPForNextLevel { get; private set; }
        public int TotalXP { get; private set; }

        public float Progress => XPForNextLevel <= 0 ? 0f : CurrentXP / (float)XPForNextLevel;

        /// <summary>Raised once per level gained, with the new level.</summary>
        public event Action<int> LevelledUp;
        public event Action Changed;

        void Awake() => XPForNextLevel = curve != null ? curve.CostFor(Level) : 20;

        public void AddXP(int amount)
        {
            if (amount <= 0) return;

            CurrentXP += amount;
            TotalXP += amount;

            // A loop, not an if: a boss dropping a large gem can carry the
            // player through more than one level at once, and each of those
            // levels still owes them a draft.
            while (CurrentXP >= XPForNextLevel)
            {
                CurrentXP -= XPForNextLevel;
                Level++;
                XPForNextLevel = curve != null ? curve.CostFor(Level) : 20;
                LevelledUp?.Invoke(Level);
            }

            Changed?.Invoke();
        }
    }
}
