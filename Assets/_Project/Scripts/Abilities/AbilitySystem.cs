using System;
using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Player;

namespace ScalePunch.Abilities
{
    /// <summary>
    /// Owns the player's abilities for one run. Passives are applied to the stat
    /// sheet as deltas when granted; actives are ticked here and fire their
    /// effect asset when their cooldown elapses.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class AbilitySystem : MonoBehaviour
    {
        [SerializeField] PlayerStats stats;
        [SerializeField] AutoShoot weapon;

        readonly List<AbilityInstance> _owned = new(16);
        readonly Dictionary<AbilityDefinition, AbilityInstance> _byDefinition = new();

        public IReadOnlyList<AbilityInstance> Owned => _owned;
        public event Action<AbilityInstance> Granted;

        void Reset()
        {
            stats = GetComponent<PlayerStats>();
            weapon = GetComponent<AutoShoot>();
        }

        void Awake()
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (weapon == null) weapon = GetComponent<AutoShoot>();
        }

        public bool Has(AbilityDefinition definition) => _byDefinition.ContainsKey(definition);

        public AbilityInstance Get(AbilityDefinition definition)
            => _byDefinition.TryGetValue(definition, out AbilityInstance instance) ? instance : null;

        /// <summary>Adds the ability at level 1, or raises it one level.</summary>
        public void Grant(AbilityDefinition definition)
        {
            if (definition == null) return;

            if (_byDefinition.TryGetValue(definition, out AbilityInstance existing))
            {
                if (existing.IsMaxed) return;

                existing.LevelUp();
                ApplyModifiers(existing.Current);
                Granted?.Invoke(existing);
                return;
            }

            var instance = new AbilityInstance(definition);
            _owned.Add(instance);
            _byDefinition[definition] = instance;

            ApplyModifiers(instance.Current);
            Granted?.Invoke(instance);
        }

        /// <summary>
        /// Applies only the new level's modifiers, not a full recomputation.
        /// Abilities never level down mid-run, so deltas are sufficient and
        /// avoid re-deriving the whole sheet on every draft.
        /// </summary>
        void ApplyModifiers(AbilityLevel level)
        {
            if (level == null || level.modifiers == null) return;

            foreach (StatModifier modifier in level.modifiers)
            {
                if (modifier.kind == ModifierKind.Percent)
                    stats.Stats.AddPercent(modifier.stat, modifier.value);
                else
                    stats.Stats.AddFlat(modifier.stat, modifier.value);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float cdr = Mathf.Clamp(stats.Get(StatType.CooldownReduction), 0f, 0.8f);
            float range = weapon != null ? weapon.Range : stats.Get(StatType.Range);

            for (int i = 0; i < _owned.Count; i++)
            {
                AbilityInstance instance = _owned[i];
                if (!instance.Tick(dt, cdr)) continue;

                AbilityEffect effect = instance.Definition.effect;
                if (effect == null) continue;

                effect.Execute(
                    new AbilityContext(transform, stats.Stats, instance.Current, gameObject, range),
                    this);
            }
        }
    }
}
