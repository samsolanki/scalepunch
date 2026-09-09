using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Enemies;

namespace ScalePunch.Abilities.Effects
{
    /// <summary>
    /// Shockwave: instant damage and knockback to everything around the player.
    /// The panic button of the set — it exists to clear a ring that is about to
    /// close, so its value is the knockback as much as the damage.
    ///
    /// Create via Assets ▸ Create ▸ ScalePunch ▸ Effects ▸ Radial Burst.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Effects/Radial Burst", fileName = "Effect_RadialBurst")]
    public class RadialBurstEffect : AbilityEffect
    {
        [SerializeField] float knockbackForce = 9f;
        [SerializeField] GameObject vfxPrefab;

        readonly List<Enemy> _hits = new(64);

        public override void Execute(AbilityContext context, MonoBehaviour runner)
        {
            Vector3 origin = context.Origin.position;

            _hits.Clear();
            EnemyRegistry.FindInRadius(origin, context.Level.radius, _hits);

            for (int i = 0; i < _hits.Count; i++)
            {
                Enemy enemy = _hits[i];
                DamageDealer.Deal(enemy, context.Damage, context.Stats, origin, context.Source);

                if (enemy.Movement != null)
                    enemy.Movement.ApplyKnockback(enemy.transform.position - origin, knockbackForce);
            }

            // Ability VFX still Instantiates. It fires once per cooldown rather
            // than per frame, so it is tolerable at M1; it joins the pooling pass
            // in M3 along with the rest of the VFX work.
            if (vfxPrefab != null) Instantiate(vfxPrefab, origin, Quaternion.identity);
        }
    }
}
