using UnityEngine;
using ScalePunch.Combat;

namespace ScalePunch.Abilities
{
    /// <summary>What an active ability does when its cooldown elapses.</summary>
    public readonly struct AbilityContext
    {
        public readonly Transform Origin;
        public readonly StatSheet Stats;
        public readonly AbilityLevel Level;
        public readonly GameObject Source;
        /// <summary>The player's engagement radius — effects that need a
        /// meaningful reach should scale off this, not a magic number.</summary>
        public readonly float EngagementRange;

        public AbilityContext(Transform origin, StatSheet stats, AbilityLevel level,
                              GameObject source, float engagementRange)
        {
            Origin = origin;
            Stats = stats;
            Level = level;
            Source = source;
            EngagementRange = engagementRange;
        }

        /// <summary>Base damage for this effect, before the crit roll.</summary>
        public float Damage => Stats.Get(StatType.Damage) * Level.damageMultiplier;
    }

    /// <summary>
    /// Subclass this to add an active ability. Effects are ScriptableObjects, so
    /// a new ability is an asset plus a small class — not a change to the
    /// ability system.
    ///
    /// Because the asset is shared, any scratch list an effect keeps as a field
    /// must be cleared and consumed without yielding in between. Do not hold
    /// per-invocation state across a `yield` — capture it in a local first.
    /// </summary>
    public abstract class AbilityEffect : ScriptableObject
    {
        /// <summary>Called on the owning MonoBehaviour so effects that need to
        /// wait (a grenade's fuse) can start a coroutine on it.</summary>
        public abstract void Execute(AbilityContext context, MonoBehaviour runner);
    }
}
