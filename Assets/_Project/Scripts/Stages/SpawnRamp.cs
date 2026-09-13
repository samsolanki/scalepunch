using System;
using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Enemies;

namespace ScalePunch.Stages
{
    /// <summary>
    /// How the endless run gets harder: which tiers are in the mix, what share
    /// each takes, how fast they arrive, and how many at a time.
    ///
    /// **Difficulty here is composition and rate, never HP inflation.** The
    /// bullet ladder (Shambler 1, Runner 2, Brute 6) is a fixed promise — the
    /// moment a Shambler needs two rounds, every tier above it has silently
    /// moved and the player's read of the whole game breaks. So a harder wave
    /// means *more* zombies and *tougher kinds* of zombie, not the same zombie
    /// with more health.
    ///
    /// Create via Assets ▸ Create ▸ ScalePunch ▸ Spawn Ramp.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Spawn Ramp", fileName = "SpawnRamp")]
    public class SpawnRamp : ScriptableObject
    {
        [Serializable]
        public class TierRamp
        {
            public EnemyDefinition enemy;

            [Tooltip("Absorbs whatever share the other tiers do not take. Exactly one " +
                     "tier should be the baseline — it is what the player fights when " +
                     "nothing else has been introduced yet.")]
            public bool isBaseline;

            [Tooltip("First wave this tier can appear at all. 1-based.")]
            public int firstWave = 1;

            [Tooltip("Wave at which this tier reaches its final share.")]
            public int rampEndWave = 10;

            [Range(0f, 1f)]
            [Tooltip("Share of spawns on the wave it is introduced. 0.4 = 40%.")]
            public float shareAtFirstWave = 0.4f;

            [Range(0f, 1f)]
            [Tooltip("Share once the ramp finishes. Flat if equal to the value above.")]
            public float shareAtRampEnd = 0.5f;

            public float ShareAt(int wave)
            {
                if (isBaseline || enemy == null) return 0f;
                if (wave < firstWave) return 0f;
                if (rampEndWave <= firstWave) return shareAtRampEnd;

                float t = Mathf.InverseLerp(firstWave, rampEndWave, wave);
                return Mathf.Lerp(shareAtFirstWave, shareAtRampEnd, t);
            }
        }

        [Header("Composition")]
        public TierRamp[] tiers = Array.Empty<TierRamp>();

        [Range(0f, 1f)]
        [Tooltip("The baseline tier never drops below this share. Letting it reach zero " +
                 "removes the one enemy the player reads every other enemy against.")]
        public float baselineMinimumShare = 0.2f;

        [Header("Spawn interval (seconds between spawn events)")]
        public float intervalAtWave1 = 1.4f;
        public float intervalAtRampEnd = 0.35f;
        public int intervalRampEndWave = 15;
        [Tooltip("Hard floor. Below roughly 0.15 s the interval stops being the lever " +
                 "and burst size has to take over.")]
        public float minimumInterval = 0.2f;

        [Header("Burst (zombies per spawn event)")]
        [Tooltip("Wave at which burst starts growing. Keep this at or after " +
                 "intervalRampEndWave: while the interval is still shortening, a " +
                 "burst step is a cliff, not a ramp. One integer step doubles the " +
                 "spawn rate in a single wave, which reads as the game breaking " +
                 "rather than getting harder.")]
        public int burstStartWave = 15;
        public int burstAtStart = 1;
        public int burstAtRampEnd = 3;
        public int burstRampEndWave = 25;

        readonly List<float> _weights = new(8);

        public float IntervalFor(int wave)
        {
            float t = intervalRampEndWave <= 1 ? 1f : Mathf.InverseLerp(1, intervalRampEndWave, wave);
            return Mathf.Max(minimumInterval, Mathf.Lerp(intervalAtWave1, intervalAtRampEnd, t));
        }

        public int BurstFor(int wave)
        {
            if (wave < burstStartWave) return Mathf.Max(1, burstAtStart);

            float t = burstRampEndWave <= burstStartWave
                ? 1f
                : Mathf.InverseLerp(burstStartWave, burstRampEndWave, wave);

            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(burstAtStart, burstAtRampEnd, t)));
        }

        /// <summary>
        /// Weighted pick for this wave. The baseline tier takes whatever the
        /// others leave, floored so it can never disappear.
        /// </summary>
        public EnemyDefinition Pick(int wave)
        {
            if (tiers == null || tiers.Length == 0) return null;

            _weights.Clear();
            float claimed = 0f;
            int baselineIndex = -1;

            for (int i = 0; i < tiers.Length; i++)
            {
                TierRamp tier = tiers[i];

                if (tier == null || tier.enemy == null) { _weights.Add(0f); continue; }
                if (tier.isBaseline) { baselineIndex = i; _weights.Add(0f); continue; }

                float share = tier.ShareAt(wave);
                _weights.Add(share);
                claimed += share;
            }

            if (baselineIndex >= 0)
                _weights[baselineIndex] = Mathf.Max(baselineMinimumShare, 1f - claimed);

            float total = 0f;
            for (int i = 0; i < _weights.Count; i++) total += _weights[i];
            if (total <= 0f) return FirstAvailable();

            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < _weights.Count; i++)
            {
                roll -= _weights[i];
                if (roll <= 0f && tiers[i] != null) return tiers[i].enemy;
            }
            return FirstAvailable();
        }

        EnemyDefinition FirstAvailable()
        {
            foreach (TierRamp tier in tiers)
                if (tier != null && tier.enemy != null) return tier.enemy;

            return null;
        }

        public IEnumerable<EnemyDefinition> AllEnemies()
        {
            if (tiers == null) yield break;

            foreach (TierRamp tier in tiers)
                if (tier != null && tier.enemy != null) yield return tier.enemy;
        }

        /// <summary>Human-readable mix at a wave, for the build-time log.</summary>
        public string Describe(int wave)
        {
            if (tiers == null || tiers.Length == 0) return "(empty)";

            float claimed = 0f;
            foreach (TierRamp tier in tiers)
                if (tier != null && !tier.isBaseline) claimed += tier.ShareAt(wave);

            var sb = new System.Text.StringBuilder();
            foreach (TierRamp tier in tiers)
            {
                if (tier == null || tier.enemy == null) continue;

                float share = tier.isBaseline
                    ? Mathf.Max(baselineMinimumShare, 1f - claimed)
                    : tier.ShareAt(wave);

                if (share <= 0f) continue;
                sb.Append($"{tier.enemy.id} {share * 100f:0}%  ");
            }
            return sb.ToString().TrimEnd();
        }

        void OnValidate()
        {
            int baselines = 0;
            if (tiers == null) return;

            foreach (TierRamp tier in tiers)
            {
                if (tier == null) continue;
                if (tier.isBaseline) baselines++;
                tier.firstWave = Mathf.Max(1, tier.firstWave);
                tier.rampEndWave = Mathf.Max(tier.firstWave, tier.rampEndWave);
            }

            // Overlapping levers is the one mistake that turns this curve into a
            // staircase, so it is worth saying out loud rather than leaving to be
            // discovered in a playtest.
            if (burstStartWave < intervalRampEndWave)
                Debug.LogWarning($"[SpawnRamp] {name}: burst starts at wave {burstStartWave} " +
                                 $"while the interval is still ramping until {intervalRampEndWave}. " +
                                 "Each burst step roughly doubles the spawn rate in one wave.", this);

            if (baselines != 1)
                Debug.LogWarning($"[SpawnRamp] {name} has {baselines} baseline tiers; " +
                                 "exactly one is needed or the mix will not sum to 1.", this);
        }
    }
}
