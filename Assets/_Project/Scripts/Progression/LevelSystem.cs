using System;
using UnityEngine;
using ScalePunch.Enemies;

namespace ScalePunch.Progression
{
    /// <summary>
    /// Run-scoped levelling. Counts whatever the curve says counts — kills by
    /// default — and raises a level-up for each threshold crossed.
    ///
    /// Owns nothing but numbers; the draft listens for LevelledUp and decides
    /// what to do about it.
    /// </summary>
    public class LevelSystem : MonoBehaviour
    {
        [SerializeField] LevelCurve curve;

        public int Level { get; private set; } = 1;
        /// <summary>Progress toward the next level, not the total.</summary>
        public int Current { get; private set; }
        public int Required { get; private set; }
        public int Total { get; private set; }

        public float Progress => Required <= 0 ? 0f : Current / (float)Required;
        public ProgressSource Source => curve != null ? curve.source : ProgressSource.Kills;

        /// <summary>Raised once per level gained, with the new level.</summary>
        public event Action<int> LevelledUp;
        public event Action Changed;

        void Awake() => Required = curve != null ? curve.CostFor(Level) : 5;

        void OnEnable()
        {
            // Only subscribe when kills are the currency — otherwise XPGemService
            // is the one feeding this and counting kills too would double-count.
            if (Source == ProgressSource.Kills) EnemyRegistry.Killed += OnKill;
        }

        void OnDisable() => EnemyRegistry.Killed -= OnKill;

        void OnKill(Enemy enemy) => Add(1);

        /// <summary>Grants progress. Called by XPGemService in XPGems mode.</summary>
        public void AddXP(int amount) => Add(amount);

        void Add(int amount)
        {
            if (amount <= 0) return;

            Current += amount;
            Total += amount;

            // A loop, not an if: a boss kill or a large gem can carry the player
            // through more than one level at once, and each of those still owes
            // them a draft.
            while (Current >= Required)
            {
                Current -= Required;
                Level++;
                Required = curve != null ? curve.CostFor(Level) : 5;
                LevelledUp?.Invoke(Level);
            }

            Changed?.Invoke();
        }
    }
}
