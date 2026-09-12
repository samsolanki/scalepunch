using UnityEngine;

namespace ScalePunch.Enemies
{
    /// <summary>
    /// Authoring intent. At M0 every zombie walks straight at the player and
    /// they differ only by stats — which is genuinely all Shambler, Runner and
    /// Brute need. Spitter needs ranged attack code and lands at M1.
    /// </summary>
    public enum EnemyBehaviour { Shambler, Runner, Brute, Spitter }

    /// <summary>
    /// Enemy balance lives here, never in code — see docs/02-tech-stack.md §3.
    /// Create via Assets > Create > ScalePunch > Enemy Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Enemy Definition", fileName = "Enemy_")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "shambler";
        public Enemy prefab;

        [Header("Base stats (wave 1, stage 1)")]
        public float baseHP = 20f;
        public float baseDamage = 8f;
        public float moveSpeed = 2.5f;
        public float attackRange = 1.2f;
        public float attackInterval = 1.0f;

        [Tooltip("Flat damage reduction per hit. This is what makes a Brute a real " +
                 "damage check rather than just a big health pool: armour punishes " +
                 "many-small-hits builds specifically.")]
        public float armor;

        [Header("Behaviour")]
        public EnemyBehaviour behaviour = EnemyBehaviour.Shambler;
        [Tooltip("1 = immune. Brutes should ignore bullet knockback entirely, or " +
                 "sustained fire trivially stunlocks them at the edge of the radius.")]
        [Range(0f, 1f)] public float knockbackResistance = 0f;

        [Header("Scaling per wave")]
        [Tooltip("HP multiplier compounded per wave. 1.0 disables scaling entirely, " +
                 "which is what keeps a \"dies in exactly N bullets\" ladder true for " +
                 "the whole run instead of only its first wave.")]
        public float hpGrowthPerWave = 1.12f;
        [Tooltip("Damage multiplier compounded per wave. 1.0 disables scaling.")]
        public float damageGrowthPerWave = 1.08f;

        [Header("Rewards")]
        public int xpValue = 1;

        [Header("Presentation")]
        public Color tint = Color.white;
        public float scale = 1f;

        /// <summary>hp(wave) = baseHP * hpGrowth^wave * stageMultiplier — docs/01 §5.</summary>
        public float HPAtWave(int wave, float stageMultiplier = 1f)
            => baseHP * Mathf.Pow(Mathf.Max(0.01f, hpGrowthPerWave), wave) * stageMultiplier;

        /// <summary>damage(wave) = baseDamage * damageGrowth^wave * stageMultiplier.</summary>
        public float DamageAtWave(int wave, float stageMultiplier = 1f)
            => baseDamage * Mathf.Pow(Mathf.Max(0.01f, damageGrowthPerWave), wave) * stageMultiplier;

        /// <summary>
        /// How many un-crit rounds of <paramref name="bulletDamage"/> this takes
        /// to kill at a given wave. Used by the builder to log the ladder, so a
        /// balance change that breaks "one bullet" is visible immediately rather
        /// than twenty seconds into a playtest.
        /// </summary>
        public int BulletsToKill(float bulletDamage, int wave = 0, float stageMultiplier = 1f)
        {
            float perHit = Mathf.Max(1f, bulletDamage - armor);
            return Mathf.CeilToInt(HPAtWave(wave, stageMultiplier) / perHit);
        }
    }
}
