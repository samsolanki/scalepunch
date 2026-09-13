namespace ScalePunch.Abilities
{
    /// <summary>
    /// One card as offered: which ability, at what rarity, and what level the
    /// player currently holds it at.
    ///
    /// The draft deals in these rather than bare definitions because rarity is
    /// rolled at offer time — the same ability is a different card depending on
    /// what it rolled.
    /// </summary>
    public readonly struct AbilityOffer
    {
        public readonly AbilityDefinition Definition;
        public readonly AbilityRarity Rarity;
        /// <summary>0 if the player does not hold it yet.</summary>
        public readonly int CurrentLevel;

        public bool IsNew => CurrentLevel == 0;
        public int NextLevel => CurrentLevel + 1;
        public float Magnitude => RarityTable.Magnitude(Rarity);

        public AbilityOffer(AbilityDefinition definition, AbilityRarity rarity, int currentLevel)
        {
            Definition = definition;
            Rarity = rarity;
            CurrentLevel = currentLevel;
        }
    }
}
