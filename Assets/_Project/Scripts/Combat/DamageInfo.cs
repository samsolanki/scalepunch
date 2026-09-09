using UnityEngine;

namespace ScalePunch.Combat
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly bool IsCrit;
        /// <summary>Where the blow came from — drives knockback direction.</summary>
        public readonly Vector3 Origin;
        public readonly GameObject Source;

        public DamageInfo(float amount, bool isCrit, Vector3 origin, GameObject source)
        {
            Amount = amount;
            IsCrit = isCrit;
            Origin = origin;
            Source = source;
        }

        /// <summary>
        /// Same blow, different number. Health uses this to re-report the damage
        /// it actually applied after armour, so the floating number matches the
        /// HP that was really lost instead of the raw roll.
        /// </summary>
        public DamageInfo WithAmount(float amount) => new DamageInfo(amount, IsCrit, Origin, Source);
    }
}
