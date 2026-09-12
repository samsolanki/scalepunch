using System;
using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Progression;

namespace ScalePunch.Enemies
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EnemyMovement))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] EnemyMovement movement;

        public EnemyDefinition Definition { get; private set; }
        public Health Health => health;
        public EnemyMovement Movement => movement;

        /// <summary>
        /// Damage already in the air toward this zombie.
        ///
        /// Without it the turret empties four rounds into a one-bullet Shambler
        /// before the first one lands, because from the targeting code's point of
        /// view the zombie is at full health until impact. Three of those rounds
        /// are wasted and the zombie behind it walks in free.
        /// </summary>
        public float IncomingDamage { get; private set; }

        /// <summary>True when enough damage is already in flight to kill it.</summary>
        public bool IsDoomed => !IsDead && IncomingDamage >= health.Current;

        public void ReserveDamage(float amount) => IncomingDamage += Mathf.Max(0f, amount);

        public void ReleaseDamage(float amount)
            => IncomingDamage = Mathf.Max(0f, IncomingDamage - Mathf.Max(0f, amount));
        public bool IsDead => health.IsDead;

        /// <summary>Raised on death so the spawner can pool it. Passed the
        /// enemy rather than captured, so one handler serves every instance.</summary>
        public event Action<Enemy> Despawned;

        void Reset()
        {
            health = GetComponent<Health>();
            movement = GetComponent<EnemyMovement>();
        }

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (movement == null) movement = GetComponent<EnemyMovement>();

            // Subscribed once for the lifetime of the pooled object.
            health.Died += OnDied;
        }

        void OnDestroy()
        {
            if (health != null) health.Died -= OnDied;
        }

        /// <summary>
        /// HP and damage take separate stage multipliers so a stage can be made
        /// spongier without also making it deadlier, which is the usual way a
        /// difficulty curve gets tuned.
        /// </summary>
        public void Spawn(EnemyDefinition definition, int wave, float hpMultiplier,
                          float damageMultiplier, Transform target)
        {
            Definition = definition;

            // Pooled: a stale reservation from a previous life would make a fresh
            // zombie look doomed and never be shot at.
            IncomingDamage = 0f;

            health.Init(definition.HPAtWave(wave, hpMultiplier), definition.armor);
            movement.Configure(definition, definition.DamageAtWave(wave, damageMultiplier), target);

            transform.localScale = Vector3.one * definition.scale;

            EnemyRegistry.Register(this);
        }

        void OnDied(DamageInfo info)
        {
            // Only a kill pays out. Despawn() is also reached by cleanup paths,
            // and those must not mint XP.
            if (XPGemService.Exists && Definition != null)
                XPGemService.Instance.Drop(transform.position, Definition.xpValue);

            EnemyRegistry.ReportKill(this);
            Despawn();
        }

        public void Despawn()
        {
            EnemyRegistry.Unregister(this);
            Despawned?.Invoke(this);
        }

        void OnDisable() => EnemyRegistry.Unregister(this);
    }
}
