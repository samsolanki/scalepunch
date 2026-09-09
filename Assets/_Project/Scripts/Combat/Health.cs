using System;
using UnityEngine;

namespace ScalePunch.Combat
{
    public class Health : MonoBehaviour
    {
        [SerializeField] float maxHP = 100f;

        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsDead { get; private set; }
        public float Normalised => Max <= 0f ? 0f : Current / Max;

        /// <summary>Raised after HP is reduced, only when the blow did not kill.</summary>
        public event Action<DamageInfo> Damaged;
        /// <summary>Raised exactly once per life, on the killing blow.</summary>
        public event Action<DamageInfo> Died;

        void Awake() => Init(maxHP);

        public void Init(float max)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
            IsDead = false;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;

            Current -= Mathf.Max(1f, info.Amount);

            if (Current <= 0f)
            {
                Current = 0f;
                IsDead = true;
                Died?.Invoke(info);
            }
            else
            {
                Damaged?.Invoke(info);
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
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(Max, Current + amount);
        }
    }
}
