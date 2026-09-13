using UnityEngine;

namespace ScalePunch.Abilities
{
    /// <summary>One ability the player currently owns, at its current level.</summary>
    public class AbilityInstance
    {
        public readonly AbilityDefinition Definition;
        public int Level { get; private set; }
        public float CooldownRemaining { get; private set; }

        /// <summary>
        /// Magnitude multiplier from the rarity this was taken at. Kept as the
        /// best roll rather than the latest: taking a Legendary and then a Common
        /// of the same ability should never be a downgrade, or the draft starts
        /// punishing the player for levelling something up.
        /// </summary>
        public float Magnitude { get; private set; } = 1f;

        public void RecordMagnitude(float magnitude)
            => Magnitude = Mathf.Max(Magnitude, magnitude);

        public AbilityLevel Current => Definition.LevelData(Level);
        public bool IsMaxed => Level >= Definition.MaxLevel;
        public bool IsActive => Definition.kind == AbilityKind.Active;

        public AbilityInstance(AbilityDefinition definition)
        {
            Definition = definition;
            Level = 1;
        }

        public void LevelUp()
        {
            if (!IsMaxed) Level++;
        }

        /// <summary>Ticks the cooldown. Returns true on the frame it comes up.</summary>
        public bool Tick(float deltaTime, float cooldownReduction)
        {
            if (!IsActive) return false;

            CooldownRemaining -= deltaTime;
            if (CooldownRemaining > 0f) return false;

            float cooldown = Current != null ? Current.cooldown : 5f;
            CooldownRemaining = Mathf.Max(0.1f, cooldown * (1f - cooldownReduction));
            return true;
        }

        public float CooldownProgress
        {
            get
            {
                float cooldown = Current != null ? Current.cooldown : 5f;
                return cooldown <= 0f ? 1f : 1f - Mathf.Clamp01(CooldownRemaining / cooldown);
            }
        }
    }
}
