using UnityEngine;
using ScalePunch.Combat;

namespace ScalePunch.Abilities
{
    /// <summary>
    /// Works out the "0.75 → 0.68" line on a draft card.
    ///
    /// Computed from the live stat sheet rather than written by hand in the
    /// asset. Hand-written effect text is stale the moment a number changes, and
    /// it cannot show the player their *own* current value — which is the whole
    /// reason the line is worth showing.
    /// </summary>
    public readonly struct AbilityPreview
    {
        public readonly string Label;
        public readonly string Before;
        public readonly string After;
        /// <summary>False for a brand-new active, where there is no "before".</summary>
        public readonly bool HasBefore;

        public AbilityPreview(string label, string before, string after, bool hasBefore)
        {
            Label = label;
            Before = before;
            After = after;
            HasBefore = hasBefore;
        }

        public static AbilityPreview Build(AbilityOffer offer, StatSheet stats)
        {
            AbilityLevel level = offer.Definition.LevelData(offer.NextLevel);
            if (level == null) return new AbilityPreview("", "", "", false);

            return offer.Definition.kind == AbilityKind.Passive
                ? BuildPassive(offer, level, stats)
                : BuildActive(offer, level, stats);
        }

        /// <summary>
        /// Previews the first modifier. A passive that changes several stats at
        /// once cannot be summarised in one row, and the honest fix is to author
        /// it as one stat per card — not to pick a stat arbitrarily here.
        /// </summary>
        static AbilityPreview BuildPassive(AbilityOffer offer, AbilityLevel level, StatSheet stats)
        {
            if (level.modifiers == null || level.modifiers.Length == 0)
                return new AbilityPreview("", "", "", false);

            StatModifier modifier = level.modifiers[0];
            bool percent = modifier.kind == ModifierKind.Percent;
            float amount = modifier.value * offer.Magnitude;

            float before = stats.Get(modifier.stat);
            float after = stats.Preview(modifier.stat, percent, amount);

            return new AbilityPreview(
                StatFormat.DisplayName(modifier.stat),
                StatFormat.Value(modifier.stat, before),
                StatFormat.Value(modifier.stat, after),
                true);
        }

        /// <summary>
        /// Actives preview their damage per activation, which is the number that
        /// actually moves when the card is taken.
        /// </summary>
        static AbilityPreview BuildActive(AbilityOffer offer, AbilityLevel level, StatSheet stats)
        {
            float playerDamage = stats.Get(StatType.Damage);
            float after = playerDamage * level.damageMultiplier * offer.Magnitude;

            if (offer.IsNew)
                return new AbilityPreview("Damage", "—", $"{after:0.#}", false);

            AbilityLevel current = offer.Definition.LevelData(offer.CurrentLevel);
            float before = current != null ? playerDamage * current.damageMultiplier : 0f;

            return new AbilityPreview("Damage", $"{before:0.#}", $"{after:0.#}", true);
        }
    }
}
