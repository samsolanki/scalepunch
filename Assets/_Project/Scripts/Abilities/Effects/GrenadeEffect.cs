using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Enemies;

namespace ScalePunch.Abilities.Effects
{
    /// <summary>
    /// Lobs at the densest cluster rather than at the nearest zombie — a grenade
    /// that lands on one shambler while a pack walks in behind it feels stupid,
    /// and players read that as the ability being weak rather than mis-aimed.
    ///
    /// Create via Assets ▸ Create ▸ ScalePunch ▸ Effects ▸ Grenade.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Effects/Grenade", fileName = "Effect_Grenade")]
    public class GrenadeEffect : AbilityEffect
    {
        [Tooltip("Fuse in seconds. A visible delay is what makes the blast readable.")]
        [SerializeField] float fuseSeconds = 0.55f;
        [SerializeField] GameObject grenadePrefab;
        [SerializeField] GameObject explosionPrefab;

        readonly List<Enemy> _candidates = new(64);
        readonly List<Enemy> _caught = new(64);

        public override void Execute(AbilityContext context, MonoBehaviour runner)
        {
            Vector3 target = FindDensestCluster(context);
            if (target == Vector3.positiveInfinity) return;

            runner.StartCoroutine(Detonate(context, target));
        }

        Vector3 FindDensestCluster(AbilityContext context)
        {
            _candidates.Clear();
            EnemyRegistry.FindInRadius(context.Origin.position, context.EngagementRange, _candidates);
            if (_candidates.Count == 0) return Vector3.positiveInfinity;

            float blastSqr = context.Level.radius * context.Level.radius;
            Vector3 best = _candidates[0].transform.position;
            int bestCount = -1;

            // O(n²) over candidates, but candidates are only what is inside the
            // engagement radius and this runs once per cooldown, not per frame.
            for (int i = 0; i < _candidates.Count; i++)
            {
                Vector3 centre = _candidates[i].transform.position;
                int count = 0;

                for (int j = 0; j < _candidates.Count; j++)
                    if ((_candidates[j].transform.position - centre).sqrMagnitude <= blastSqr) count++;

                if (count <= bestCount) continue;
                bestCount = count;
                best = centre;
            }
            return best;
        }

        IEnumerator Detonate(AbilityContext context, Vector3 target)
        {
            GameObject grenade = grenadePrefab != null
                ? Instantiate(grenadePrefab, context.Origin.position, Quaternion.identity)
                : null;

            float elapsed = 0f;
            Vector3 from = context.Origin.position;

            while (elapsed < fuseSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fuseSeconds);

                if (grenade != null)
                {
                    // Parabolic arc, so the throw reads as a throw.
                    Vector3 flat = Vector3.Lerp(from, target, t);
                    flat.y += Mathf.Sin(t * Mathf.PI) * 2.5f;
                    grenade.transform.position = flat;
                }
                yield return null;
            }

            if (grenade != null) Destroy(grenade);
            if (explosionPrefab != null) Instantiate(explosionPrefab, target, Quaternion.identity);

            _caught.Clear();
            EnemyRegistry.FindInRadius(target, context.Level.radius, _caught);

            for (int i = 0; i < _caught.Count; i++)
                DamageDealer.Deal(_caught[i], context.Damage, context.Stats, target, context.Source);
        }
    }
}
