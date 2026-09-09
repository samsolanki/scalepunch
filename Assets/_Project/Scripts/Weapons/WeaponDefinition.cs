using UnityEngine;
using ScalePunch.Combat;

namespace ScalePunch.Weapons
{
    /// <summary>
    /// A weapon is a set of multipliers over the player's stat sheet, never a
    /// set of absolute numbers. That way a pistol and a minigun scale off the
    /// same upgrade tree instead of needing parallel balance passes.
    ///
    /// Create via Assets ▸ Create ▸ ScalePunch ▸ Weapon Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Weapon Definition", fileName = "Weapon_")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "pistol";
        public string displayName = "Pistol";

        [Header("Projectile")]
        public Projectile projectilePrefab;
        [Tooltip("Radius of the round's body for hit detection, in metres.")]
        public float hitRadius = 0.35f;
        [Tooltip("Seconds before an unspent round despawns. Backstop only — range should expire it first.")]
        public float lifetime = 2.5f;

        [Header("Multipliers over the player's stats")]
        public float damageMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public float rangeMultiplier = 1f;
        public float projectileSpeedMultiplier = 1f;

        [Header("Shot shape")]
        [Tooltip("Extra rounds per shot on top of the player's ProjectileCount.")]
        public int extraProjectiles = 0;
        [Tooltip("Total cone width in degrees across all rounds in one shot. 0 = perfectly accurate.")]
        [Range(0f, 90f)] public float spreadDegrees = 0f;
        [Tooltip("Random angular error applied per round. Keep small; this is felt as unfairness.")]
        [Range(0f, 15f)] public float inaccuracyDegrees = 1.5f;

        [Header("Homing")]
        [Tooltip("Degrees per second a round may steer toward its target. 0 = dumb-fire. " +
                 "A little steering hides aim error against runners without feeling like a lock-on.")]
        public float steerDegreesPerSecond = 220f;

        public float DamageFor(StatSheet stats) => stats.Get(StatType.Damage) * damageMultiplier;
        public float FireRateFor(StatSheet stats) => Mathf.Max(0.05f, stats.Get(StatType.FireRate) * fireRateMultiplier);
        public float RangeFor(StatSheet stats) => stats.Get(StatType.Range) * rangeMultiplier;
        public float SpeedFor(StatSheet stats) => stats.Get(StatType.ProjectileSpeed) * projectileSpeedMultiplier;
    }
}
