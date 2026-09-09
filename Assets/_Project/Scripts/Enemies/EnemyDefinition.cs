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

        [Header("Rewards")]
        public int xpValue = 1;

        [Header("Presentation")]
        public Color tint = Color.white;
        public float scale = 1f;

        /// <summary>hp(wave) = baseHP * 1.12^wave * stageMultiplier — docs/01 §5.</summary>
        public float HPAtWave(int wave, float stageMultiplier = 1f)
            => baseHP * Mathf.Pow(1.12f, wave) * stageMultiplier;

        /// <summary>damage(wave) = baseDamage * 1.08^wave * stageMultiplier.</summary>
        public float DamageAtWave(int wave, float stageMultiplier = 1f)
            => baseDamage * Mathf.Pow(1.08f, wave) * stageMultiplier;
    }
}
