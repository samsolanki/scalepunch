namespace ScalePunch.Combat
{
    /// <summary>The complete stat vocabulary. Abilities, gear and base rooms
    /// may only ever modify one of these — see docs/01-game-design.md §3.</summary>
    public enum StatType
    {
        MaxHP,
        /// <summary>Damage per projectile, before the weapon's multiplier.</summary>
        Damage,
        /// <summary>Shots per second, before the weapon's multiplier.</summary>
        FireRate,
        /// <summary>The engagement radius. A zombie inside this gets shot.</summary>
        Range,
        CritChance,
        CritMultiplier,
        Armor,
        Lifesteal,
        PickupRadius,
        CooldownReduction,
        Luck,
        /// <summary>Metres per second. Faster rounds miss runners less often.</summary>
        ProjectileSpeed,
        /// <summary>Rounds per shot. Fractional values roll for the extra round.</summary>
        ProjectileCount,
        /// <summary>Extra zombies each round passes through before expiring.</summary>
        Pierce,
        /// <summary>Reserved: the player does not move at M0, but repositioning
        /// upgrades are plausible later and gear may already roll this.</summary>
        MoveSpeed
    }
}
