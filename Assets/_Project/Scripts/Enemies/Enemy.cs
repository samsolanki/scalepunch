using System;
using UnityEngine;
using ScalePunch.Combat;

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

        public void Spawn(EnemyDefinition definition, int wave, float stageMultiplier, Transform target)
        {
            Definition = definition;

            health.Init(definition.HPAtWave(wave, stageMultiplier));
            movement.Configure(definition, definition.DamageAtWave(wave, stageMultiplier), target);

            transform.localScale = Vector3.one * definition.scale;

            EnemyRegistry.Register(this);
        }

        void OnDied(DamageInfo info) => Despawn();

        public void Despawn()
        {
            EnemyRegistry.Unregister(this);
            Despawned?.Invoke(this);
        }

        void OnDisable() => EnemyRegistry.Unregister(this);
    }
}
