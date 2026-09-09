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
    }
}
