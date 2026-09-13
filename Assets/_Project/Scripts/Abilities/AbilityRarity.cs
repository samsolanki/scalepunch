using UnityEngine;

namespace ScalePunch.Abilities
{
    public enum AbilityRarity { Common, Uncommon, Rare, Epic, Legendary }

    /// <summary>
    /// Rarity is rolled per card, not stored per ability.
    ///
    /// That is the whole point: the same ability offered twice should feel
    /// different. "+15% damage (Common)" and "+38% damage (Epic)" are the same
    /// card doing very different work, and without that second axis a draft pool
    /// of eight abilities goes stale in about three runs.
    ///
    /// Rarity scales magnitude only — never cooldowns. A Legendary that also
    /// fires twice as often compounds two multipliers into a number nobody
    /// balanced.
    /// </summary>
    public static class RarityTable
    {
        public struct Tier
        {
            public string label;
            /// <summary>Relative draft weight before Luck.</summary>
            public float weight;
            /// <summary>Multiplier on the ability's authored magnitude.</summary>
            public float magnitude;
            public Color colour;
            /// <summary>Epic and above get the glow treatment.</summary>
            public bool glows;
        }

        static readonly Tier[] Tiers =
        {
            new() { label = "COMMON",    weight = 50f, magnitude = 1.00f,
                    colour = new Color(0.62f, 0.65f, 0.69f), glows = false },
            new() { label = "UNCOMMON",  weight = 26f, magnitude = 1.35f,
                    colour = new Color(0.30f, 0.78f, 0.37f), glows = false },
            new() { label = "RARE",      weight = 14f, magnitude = 1.80f,
                    colour = new Color(0.23f, 0.61f, 0.91f), glows = false },
            new() { label = "EPIC",      weight = 7f,  magnitude = 2.50f,
                    colour = new Color(0.91f, 0.29f, 0.78f), glows = true },
            new() { label = "LEGENDARY", weight = 3f,  magnitude = 3.50f,
                    colour = new Color(0.96f, 0.65f, 0.14f), glows = true }
        };

        public static int Count => Tiers.Length;

        public static Tier Of(AbilityRarity rarity) => Tiers[Mathf.Clamp((int)rarity, 0, Tiers.Length - 1)];

        public static string Label(AbilityRarity rarity) => Of(rarity).label;
        public static Color Colour(AbilityRarity rarity) => Of(rarity).colour;
        public static float Magnitude(AbilityRarity rarity) => Of(rarity).magnitude;
        public static bool Glows(AbilityRarity rarity) => Of(rarity).glows;

        /// <summary>
        /// Rolls a rarity. Luck shifts weight up the table — this is what the
        /// Luck stat has always been for (docs/01-game-design.md §3).
        /// </summary>
        public static AbilityRarity Roll(float luck = 0f, float luckScale = 0.35f)
        {
            float total = 0f;
            for (int i = 0; i < Tiers.Length; i++) total += WeightOf(i, luck, luckScale);

            float roll = Random.value * total;

            for (int i = 0; i < Tiers.Length; i++)
            {
                roll -= WeightOf(i, luck, luckScale);
                if (roll <= 0f) return (AbilityRarity)i;
            }
            return AbilityRarity.Common;
        }

        /// <summary>
        /// Higher tiers gain more from Luck than lower ones, so Luck raises the
        /// ceiling rather than just nudging everything evenly.
        /// </summary>
        static float WeightOf(int tierIndex, float luck, float luckScale)
            => Tiers[tierIndex].weight * (1f + luck * tierIndex * luckScale);
    }
}
