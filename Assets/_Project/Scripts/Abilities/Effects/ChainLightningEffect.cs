using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Enemies;

namespace ScalePunch.Abilities.Effects
{
    /// <summary>
    /// Arcs from the nearest zombie outward, hopping to the nearest unhit
    /// neighbour each time. Rewards dense waves, which is exactly the situation
    /// single-target fire handles worst.
    ///
    /// Create via Assets ▸ Create ▸ ScalePunch ▸ Effects ▸ Chain Lightning.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Effects/Chain Lightning", fileName = "Effect_ChainLightning")]
    public class ChainLightningEffect : AbilityEffect
    {
        [Tooltip("How far the arc may jump between zombies.")]
        [SerializeField] float chainRange = 4f;
        [Tooltip("Damage retained per hop. Below 1 the chain visibly peters out, " +
                 "which keeps long chains exciting rather than mandatory.")]
        [Range(0.3f, 1f)] [SerializeField] float damageFalloff = 0.85f;
        [SerializeField] LineRenderer arcPrefab;
        [SerializeField] float arcVisibleSeconds = 0.12f;

        readonly List<Enemy> _chain = new(16);

        public override void Execute(AbilityContext context, MonoBehaviour runner)
        {
            Vector3 origin = context.Origin.position;

            _chain.Clear();
            Enemy current = EnemyRegistry.FindNearest(origin, context.EngagementRange);
            if (current == null) return;

            float damage = context.Damage;
            Vector3 from = origin;

            for (int hop = 0; hop < context.Level.targets && current != null; hop++)
            {
                _chain.Add(current);
                DamageDealer.Deal(current, damage, context.Stats, from, context.Source);

                if (arcPrefab != null) DrawArc(runner, from, current.transform.position);

                from = current.transform.position;
                damage *= damageFalloff;
                current = EnemyRegistry.FindNearestExcluding(from, chainRange, _chain);
            }
        }

        void DrawArc(MonoBehaviour runner, Vector3 from, Vector3 to)
        {
            LineRenderer arc = Instantiate(arcPrefab, from, Quaternion.identity);
            arc.useWorldSpace = true;
            arc.positionCount = 2;
            arc.SetPosition(0, from + Vector3.up);
            arc.SetPosition(1, to + Vector3.up);

            Destroy(arc.gameObject, arcVisibleSeconds);
        }
    }
}
