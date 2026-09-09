using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Enemies;

namespace ScalePunch.Player
{
    /// <summary>
    /// The core verb. Finds the nearest enemy on a cooldown, faces it, and
    /// damages everything inside a forward cone — a punch connects with the
    /// crowd, not a single target, which is what makes wave clearing feel good.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class AutoAttack : MonoBehaviour
    {
        [SerializeField] PlayerStats stats;
        [SerializeField] Transform model;
        [SerializeField] Transform punchOrigin;

        [Header("Swing shape")]
        [Tooltip("Half-angle of the punch cone. 60 = a generous 120-degree arc.")]
        [Range(10f, 180f)] [SerializeField] float halfAngle = 60f;
        [Tooltip("Extra reach beyond attackRange, so the cone feels as big as it looks.")]
        [SerializeField] float rangePadding = 0.4f;
        [SerializeField] int maxTargetsPerSwing = 8;

        [Header("Debug")]
        [SerializeField] bool drawGizmos = true;

        readonly List<Enemy> _hits = new(32);
        Health _health;
        float _cooldown;

        public Enemy CurrentTarget { get; private set; }
        /// <summary>Normalised progress toward the next swing, for UI.</summary>
        public float SwingProgress { get; private set; }

        void Reset()
        {
            stats = GetComponent<PlayerStats>();
            punchOrigin = transform;
        }

        void Awake()
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (punchOrigin == null) punchOrigin = transform;
            _health = GetComponent<Health>();
        }

        void Update()
        {
            float interval = 1f / Mathf.Max(0.05f, stats.Get(StatType.AttackSpeed));

            _cooldown -= Time.deltaTime;
            SwingProgress = Mathf.Clamp01(1f - _cooldown / interval);
            if (_cooldown > 0f) return;

            float range = stats.Get(StatType.AttackRange);
            CurrentTarget = EnemyRegistry.FindNearest(punchOrigin.position, range + rangePadding);
            if (CurrentTarget == null) return;

            FaceTarget();
            Swing(range + rangePadding);
            _cooldown = interval;
        }

        void FaceTarget()
        {
            if (model == null) return;

            Vector3 toTarget = CurrentTarget.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f) return;

            model.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        void Swing(float range)
        {
            Vector3 forward = model != null ? model.forward : transform.forward;

            _hits.Clear();
            EnemyRegistry.FindInCone(punchOrigin.position, forward, range, halfAngle, _hits);
            if (_hits.Count == 0) return;

            float baseDamage = stats.Get(StatType.Damage);
            float critChance = stats.Get(StatType.CritChance);
            float critMult = stats.Get(StatType.CritMultiplier);
            float lifesteal = stats.Get(StatType.Lifesteal);

            int count = Mathf.Min(_hits.Count, maxTargetsPerSwing);
            float dealt = 0f;

            for (int i = 0; i < count; i++)
            {
                Enemy enemy = _hits[i];
                if (enemy == null || enemy.IsDead) continue;

                // Crit is rolled per target, so a wide swing can crit some and
                // not others — reads as far more varied than one roll per swing.
                bool isCrit = Random.value < critChance;
                float amount = baseDamage * (isCrit ? critMult : 1f);

                enemy.Health.TakeDamage(new DamageInfo(amount, isCrit, punchOrigin.position, gameObject));
                dealt += amount;
            }

            if (lifesteal > 0f && dealt > 0f && _health != null)
                _health.Heal(dealt * lifesteal);
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmos || stats == null || !Application.isPlaying) return;

            float range = stats.Get(StatType.AttackRange) + rangePadding;
            Vector3 forward = model != null ? model.forward : transform.forward;
            Vector3 origin = punchOrigin != null ? punchOrigin.position : transform.position;

            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
            Gizmos.DrawRay(origin, Quaternion.Euler(0f, halfAngle, 0f) * forward * range);
            Gizmos.DrawRay(origin, Quaternion.Euler(0f, -halfAngle, 0f) * forward * range);
        }
    }
}
