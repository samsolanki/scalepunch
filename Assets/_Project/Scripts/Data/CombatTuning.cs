using UnityEngine;

namespace ScalePunch.Data
{
    /// <summary>
    /// Every feel number in one asset so it can be tuned in play mode without
    /// hunting through prefabs. This asset is the M0 exit test —
    /// docs/03-roadmap.md.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Combat Tuning", fileName = "CombatTuning")]
    public class CombatTuning : ScriptableObject
    {
        [Header("Hitstop (seconds, unscaled)")]
        [Range(0f, 0.2f)] public float hitstopNormal = 0.04f;
        [Range(0f, 0.2f)] public float hitstopCrit = 0.07f;
        [Range(0f, 0.4f)] public float hitstopKill = 0.09f;
        [Tooltip("Time scale held during hitstop. Never exactly 0 — a total freeze reads as a hitch.")]
        [Range(0f, 0.5f)] public float hitstopTimeScale = 0.05f;

        [Header("Screen shake")]
        public float shakeNormal = 0.10f;
        public float shakeCrit = 0.22f;
        public float shakeKill = 0.16f;
        public float shakeDuration = 0.18f;

        [Header("Knockback")]
        public float knockbackNormal = 4f;
        public float knockbackCrit = 7f;

        [Header("Hit flash")]
        [Range(0f, 0.5f)] public float flashDuration = 0.08f;
        public Color flashColour = Color.white;

        [Header("Damage numbers")]
        public Color normalColour = new(1f, 0.95f, 0.8f);
        public Color critColour = new(1f, 0.72f, 0.2f);
        public float numberLifetime = 0.7f;
        public float numberRise = 70f;
        public float numberSpread = 45f;
        public float critScale = 1.5f;
    }
}
