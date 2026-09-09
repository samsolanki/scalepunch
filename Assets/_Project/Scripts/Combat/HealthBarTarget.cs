using System.Collections.Generic;
using UnityEngine;

namespace ScalePunch.Combat
{
    /// <summary>
    /// Marks anything with a <see cref="Health"/> as wanting a floating bar.
    /// Identical on the player and on zombies - the bar system does not know or
    /// care which it is drawing.
    ///
    /// Registration is static and self-managing, the same pattern as
    /// EnemyRegistry, so pooled zombies come and go without anything having to
    /// track them.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HealthBarTarget : MonoBehaviour
    {
        [SerializeField] Health health;

        [Tooltip("Metres above the transform to float the bar.")]
        [SerializeField] float heightOffset = 2.1f;

        [Tooltip("Width in reference-resolution pixels.")]
        [SerializeField] float width = 90f;

        [Tooltip("Hide while untouched. A screen of full bars is noise - a bar " +
                 "should mean 'this one is hurt'.")]
        [SerializeField] bool hideWhenFull = true;

        [Tooltip("Drawn ahead of lower values when more targets are hurt than " +
                 "there are bars to go round. The player outranks every zombie.")]
        [SerializeField] int priority;

        static readonly List<HealthBarTarget> Active = new(128);

        public static IReadOnlyList<HealthBarTarget> All => Active;

        public Health Health => health;
        public float HeightOffset => heightOffset;
        public float Width => width;
        public bool HideWhenFull => hideWhenFull;
        public int Priority => priority;

        public bool WantsBar =>
            health != null && !health.IsDead && (!hideWhenFull || health.Normalised < 0.999f);

        void Reset() => health = GetComponent<Health>();

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        /// <summary>Statics outlive scene loads; the run must clear them or the
        /// next run starts with bars for zombies that no longer exist.</summary>
        public static void Clear() => Active.Clear();
    }
}
