using System;
using UnityEngine;
using ScalePunch.Combat;

namespace ScalePunch.Abilities
{
    public enum AbilityKind
    {
        /// <summary>Modifies the stat sheet. No cooldown, no effect asset.</summary>
        Passive,
        /// <summary>Fires an effect on a cooldown, automatically.</summary>
        Active
    }

    public enum ModifierKind { Flat, Percent }

    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public ModifierKind kind;
        [Tooltip("Percent modifiers are fractions: 0.15 = +15%.")]
        public float value;
    }

    /// <summary>One level of an ability. Actives read the combat fields; passives
    /// read the modifier list. Both read the description, which is what the
    /// player actually decides on.</summary>
    [Serializable]
    public class AbilityLevel
    {
        [TextArea(2, 3)]
        [Tooltip("Shown on the draft card. Write the number, not the adjective — " +
                 "\"+15% fire rate\" beats \"shoots faster\".")]
        public string description;

        [Header("Passive")]
        public StatModifier[] modifiers = Array.Empty<StatModifier>();

        [Header("Active")]
        public float cooldown = 5f;
        [Tooltip("Multiplier over the player's Damage stat, so actives scale with the build.")]
        public float damageMultiplier = 1f;
        public float radius = 3f;
        [Tooltip("Chains, bomblets, or targets, depending on the effect.")]
        public int targets = 3;
    }

    /// <summary>
    /// Create via Assets ▸ Create ▸ ScalePunch ▸ Ability Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Ability Definition", fileName = "Ability_")]
    public class AbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public Sprite icon;
        public AbilityKind kind = AbilityKind.Passive;

        [Header("Levels")]
        [Tooltip("One entry per level. Length is the max level — five is the genre standard.")]
        public AbilityLevel[] levels = new AbilityLevel[5];

        [Header("Active only")]
        public AbilityEffect effect;

        [Header("Draft")]
        [Tooltip("Relative likelihood of appearing in a draft. Range upgrades " +
                 "should sit well below 1 — see docs/01-game-design.md §4.2.")]
        public float draftWeight = 1f;

        [Header("Evolution (M3)")]
        [Tooltip("Maxing this ability while holding the passive below offers the evolution.")]
        public AbilityDefinition evolvesInto;
        public AbilityDefinition evolutionRequires;
        [Tooltip("Evolutions are never offered directly; they are unlocked by their prerequisites.")]
        public bool isEvolution;

        public int MaxLevel => levels != null ? levels.Length : 0;

        public AbilityLevel LevelData(int level)
        {
            if (levels == null || levels.Length == 0) return null;
            return levels[Mathf.Clamp(level, 1, levels.Length) - 1];
        }

        void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
            if (string.IsNullOrEmpty(displayName)) displayName = name.Replace("Ability_", "");
            draftWeight = Mathf.Max(0f, draftWeight);
        }
    }
}
