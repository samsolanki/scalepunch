using UnityEngine;

namespace ScalePunch.Enemies
{
    public enum EnemyBehaviour { Chase, Runner, Brute }

    /// <summary>
    /// Enemy balance lives here, never in code — see docs/02-tech-stack.md §3.
    /// Create via Assets > Create > ScalePunch > Enemy Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Enemy Definition", fileName = "Enemy_")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "grunt";
        public Enemy prefab;

        [Header("Base stats (wave 1, stage 1)")]
        public float baseHP = 20f;
        public float baseDamage = 8f;
        public float moveSpeed = 2.5f;
        public float attackRange = 1.2f;
        public float attackInterval = 1.0f;

        [Header("Behaviour")]
        public EnemyBehaviour behaviour = EnemyBehaviour.Chase;
        [Tooltip("Brutes ignore knockback entirely.")]
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
