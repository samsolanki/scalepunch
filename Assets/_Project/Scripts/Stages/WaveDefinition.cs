using System;
using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Enemies;

namespace ScalePunch.Stages
{
    public enum SpawnPattern
    {
        /// <summary>Spread evenly all the way around the player. The default —
        /// it is what makes a fixed emplacement feel surrounded.</summary>
        Ring,
        /// <summary>Clustered into one random arc. Creates a pressure direction,
        /// which is the only way a stationary player can be "flanked".</summary>
        Arc,
        /// <summary>All from one point. Reads as a breach.</summary>
        Point,
        /// <summary>Random angles, no structure. Use sparingly; it looks like
        /// noise rather than design.</summary>
        Scatter
    }

    [Serializable]
    public struct WaveEntry
    {
        public EnemyDefinition enemy;
        [Tooltip("Seconds into this wave when the group starts arriving.")]
        public float timeOffset;
        public int count;
        public SpawnPattern pattern;
        [Tooltip("Stagger the group over this many seconds. 0 drops all of them at once, " +
                 "which is a spike, not a wave — use it deliberately.")]
        public float spreadSeconds;
        [Tooltip("Arc and Point only: width of the arc in degrees.")]
        public float arcDegrees;
    }

    /// <summary>
    /// One wave: what arrives, from where, and when.
    ///
    /// Create via Assets ▸ Create ▸ ScalePunch ▸ Wave Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Wave Definition", fileName = "Wave_")]
    public class WaveDefinition : ScriptableObject
    {
        public string displayName = "Wave";
        [Tooltip("How long this wave lasts before the next begins. Must be at least " +
                 "as long as the last entry's timeOffset + spreadSeconds, or that " +
                 "group is cut off mid-arrival.")]
        public float duration = 20f;
        public WaveEntry[] entries = Array.Empty<WaveEntry>();

        /// <summary>Every distinct enemy this wave can spawn, for pool pre-warming.</summary>
        public void CollectEnemies(HashSet<EnemyDefinition> into)
        {
            if (entries == null) return;

            foreach (WaveEntry entry in entries)
                if (entry.enemy != null) into.Add(entry.enemy);
        }

        /// <summary>Latest moment anything in this wave is still arriving. The
        /// wave cannot end before this or spawns are silently dropped.</summary>
        public float LastArrival()
        {
            float latest = 0f;
            if (entries == null) return latest;

            foreach (WaveEntry entry in entries)
                latest = Mathf.Max(latest, entry.timeOffset + Mathf.Max(0f, entry.spreadSeconds));

            return latest;
        }

        void OnValidate()
        {
            // A wave shorter than its own contents drops spawns with no error,
            // which is close to impossible to diagnose from in-game symptoms.
            float needed = LastArrival();
            if (duration < needed) duration = needed;
        }
    }
}
