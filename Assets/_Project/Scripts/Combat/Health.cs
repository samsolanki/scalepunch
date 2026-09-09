using System;
using UnityEngine;

namespace ScalePunch.Combat
{
    /// <summary>
    /// The single health component, used unchanged by the player and by every
    /// zombie. Nothing else in the project tracks hit points.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] float maxHP = 100f;
        [Tooltip("Flat damage reduction applied before HP is lost.")]
        [SerializeField] float armor;

        public float Max { get; private set; }
        public float Current { get; private set; }
        public float Armor { get; private set; }
        public bool IsDead { get; private set; }
        public float Normalised => Max <= 0f ? 0f : Current / Max;

        /// <summary>Raised after HP is reduced, only when the blow did not kill.</summary>
        public event Action<DamageInfo> Damaged;
        /// <summary>Raised exactly once per life, on the killing blow.</summary>
        public event Action<DamageInfo> Died;
        /// <summary>Raised on any change to current or max HP, including heals
        /// and cap changes. Health bars listen to this rather than polling.</summary>
        public event Action<Health> Changed;

        void Awake() => Init(maxHP, armor);

        public void Init(float max, float armour = 0f)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
            Armor = Mathf.Max(0f, armour);
            IsDead = false;

            Changed?.Invoke(this);
        }

        public void SetArmor(float value)
        {
            Armor = Mathf.Max(0f, value);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;

            // Flat reduction, floored at 1. Armour must never make a target
            // immune - something that cannot be hurt at all reads as a bug, not
            // as a tough enemy.
            float applied = Mathf.Max(1f, info.Amount - Armor);
            Current -= applied;

            // Re-report at the applied amount so the floating number matches the
            // HP actually lost rather than the pre-armour roll.
            DamageInfo resolved = info.WithAmount(applied);

            if (Current <= 0f)
            {
                Current = 0f;
                IsDead = true;
                Changed?.Invoke(this);
                Died?.Invoke(resolved);
            }
            else
            {
                Changed?.Invoke(this);
                Damaged?.Invoke(resolved);
            }
        }

        /// <summary>
        /// Changes the cap while preserving current HP. Used when a +MaxHP
        /// modifier lands mid-run — Init() would refill to full and turn every
        /// health upgrade into a free heal.
        /// </summary>
        public void SetMax(float newMax, float healBy = 0f)
        {
            Max = Mathf.Max(1f, newMax);
            Current = Mathf.Min(Max, Current + healBy);
            if (Current <= 0f && !IsDead) Current = 1f;

            Changed?.Invoke(this);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            float before = Current;
            Current = Mathf.Min(Max, Current + amount);

            // Lifesteal fires on every bullet impact; skip the event when the
            // target is already at full so listeners are not woken pointlessly.
            if (!Mathf.Approximately(before, Current)) Changed?.Invoke(this);
        }

        /// <summary>Brings a dead target back at a fraction of its maximum.
        /// The rewarded-ad revive at M4 is the caller this exists for.</summary>
        public void Revive(float fractionOfMax = 0.5f)
        {
            if (!IsDead) return;

            IsDead = false;
            Current = Mathf.Clamp(Max * fractionOfMax, 1f, Max);
            Changed?.Invoke(this);
        }
    }
}
