using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScalePunch.Combat
{
    /// <summary>
    /// Base values plus flat and percent modifiers, resolved lazily.
    /// Percent bonuses are ADDITIVE with each other, on purpose: multiplicative
    /// stacking makes the economy explode by stage 6 and cannot be walked back.
    /// </summary>
    [Serializable]
    public class StatSheet
    {
        [Serializable]
        public struct BaseValue
        {
            public StatType stat;
            public float value;
        }

        [SerializeField]
        BaseValue[] baseValues =
        {
            new() { stat = StatType.MaxHP,           value = 100f },
            // 5 damage against a 5 HP Shambler is one bullet, one kill - the
            // clearest possible read on whether the gun is working.
            new() { stat = StatType.Damage,          value = 5f   },
            new() { stat = StatType.FireRate,        value = 3.0f },
            new() { stat = StatType.Range,           value = 9.0f },
            new() { stat = StatType.CritChance,      value = 0.05f },
            new() { stat = StatType.CritMultiplier,  value = 2.0f },
            // Fast enough to read as a shot rather than a thrown pebble, slow
            // enough that the tracer is still visible crossing the ring.
            new() { stat = StatType.ProjectileSpeed, value = 45f  },
            new() { stat = StatType.ProjectileCount, value = 1f   },
            new() { stat = StatType.Pierce,          value = 0f   },
        };

        readonly Dictionary<StatType, float> _base = new();
        readonly Dictionary<StatType, float> _flat = new();
        readonly Dictionary<StatType, float> _percent = new();
        readonly Dictionary<StatType, float> _cache = new();

        bool _built;

        public event Action Changed;

        void Build()
        {
            _base.Clear();
            foreach (BaseValue bv in baseValues) _base[bv.stat] = bv.value;
            _built = true;
        }

        public float Get(StatType stat)
        {
            if (!_built) Build();
            if (_cache.TryGetValue(stat, out float cached)) return cached;

            _base.TryGetValue(stat, out float b);
            _flat.TryGetValue(stat, out float flat);
            _percent.TryGetValue(stat, out float pct);

            float value = (b + flat) * (1f + pct);
            _cache[stat] = value;
            return value;
        }

        public void AddFlat(StatType stat, float amount)
        {
            _flat.TryGetValue(stat, out float current);
            _flat[stat] = current + amount;
            Invalidate();
        }

        public void AddPercent(StatType stat, float fraction)
        {
            _percent.TryGetValue(stat, out float current);
            _percent[stat] = current + fraction;
            Invalidate();
        }

        /// <summary>Clears run-scoped modifiers. Base values survive.</summary>
        public void ResetModifiers()
        {
            _flat.Clear();
            _percent.Clear();
            Invalidate();
        }

        void Invalidate()
        {
            _cache.Clear();
            Changed?.Invoke();
        }
    }
}
