namespace ScalePunch.Combat
{
    /// <summary>The complete stat vocabulary. Abilities, gear and base rooms
    /// may only ever modify one of these — see docs/01-game-design.md §3.</summary>
    public enum StatType
    {
        MaxHP,
        Damage,
        AttackSpeed,
        AttackRange,
        MoveSpeed,
        CritChance,
        CritMultiplier,
        Armor,
        Lifesteal,
        PickupRadius,
        CooldownReduction,
        Luck
    }
}
