using UnityEngine;
using ScalePunch.Enemies;

namespace ScalePunch.Combat
{
    /// <summary>
    /// One place where a damage number becomes a crit roll and a Health call.
    /// Every source — rounds, grenades, chain lightning — goes through here, so
    /// crit behaviour cannot drift between them.
    /// </summary>
    public static class DamageDealer
    {
        /// <summary>Rolls crit from the sheet and applies the result.</summary>
        public static float Deal(Enemy target, float baseAmount, StatSheet stats,
                                 Vector3 origin, GameObject source)
        {
            if (target == null || target.IsDead) return 0f;

            bool isCrit = Random.value < stats.Get(StatType.CritChance);
            float amount = baseAmount * (isCrit ? stats.Get(StatType.CritMultiplier) : 1f);

            target.Health.TakeDamage(new DamageInfo(amount, isCrit, origin, source));
            return amount;
        }
    }
}
