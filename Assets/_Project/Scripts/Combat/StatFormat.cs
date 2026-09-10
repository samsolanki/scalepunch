using UnityEngine;

namespace ScalePunch.Combat
{
    /// <summary>
    /// Turns a stat value into the string a player reads on a card.
    ///
    /// Centralised because the same stat has to format identically everywhere —
    /// a draft card saying "5%" while the stats panel says "0.05" is the kind of
    /// inconsistency players report as a bug.
    /// </summary>
    public static class StatFormat
    {
        public static bool IsPercent(StatType stat) => stat switch
        {
            StatType.CritChance => true,
            StatType.Lifesteal => true,
            StatType.CooldownReduction => true,
            _ => false
        };

        public static string DisplayName(StatType stat) => stat switch
        {
            StatType.MaxHP => "Max Health",
            StatType.Damage => "Damage",
            StatType.FireRate => "Fire Rate",
            StatType.Range => "Range",
            StatType.CritChance => "Critical Chance",
            StatType.CritMultiplier => "Critical Damage",
            StatType.Armor => "Armour",
            StatType.Lifesteal => "Lifesteal",
            StatType.PickupRadius => "Pickup Radius",
            StatType.CooldownReduction => "Cooldown Reduction",
            StatType.Luck => "Luck",
            StatType.ProjectileSpeed => "Bullet Speed",
            StatType.ProjectileCount => "Rounds per Shot",
            StatType.Pierce => "Pierce",
            _ => stat.ToString()
        };

        public static string Value(StatType stat, float value) => stat switch
        {
            StatType.CritChance or StatType.Lifesteal or StatType.CooldownReduction
                => $"{value * 100f:0.#}%",
            StatType.CritMultiplier => $"{value:0.##}x",
            StatType.FireRate => $"{value:0.##}/s",
            StatType.Range or StatType.PickupRadius => $"{value:0.#} m",
            StatType.ProjectileSpeed => $"{value:0} m/s",
            StatType.MaxHP or StatType.Damage or StatType.Armor => $"{value:0.#}",
            StatType.Pierce or StatType.ProjectileCount => $"{value:0.##}",
            _ => $"{value:0.##}"
        };
    }
}
