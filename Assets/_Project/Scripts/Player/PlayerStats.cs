using UnityEngine;
using ScalePunch.Combat;

namespace ScalePunch.Player
{
    /// <summary>
    /// Owns the player's StatSheet and keeps Health's maximum in sync with it,
    /// so a +MaxHP ability at M1 raises current HP too rather than silently
    /// doing nothing.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] StatSheet stats = new();
        [SerializeField] Health health;

        public StatSheet Stats => stats;

        void Reset() => health = GetComponent<Health>();

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            stats.Changed += SyncHealth;
            health.Init(stats.Get(StatType.MaxHP));
        }

        void OnDestroy() => stats.Changed -= SyncHealth;

        void SyncHealth()
        {
            float newMax = stats.Get(StatType.MaxHP);
            float gained = newMax - health.Max;
            if (Mathf.Approximately(gained, 0f)) return;

            // Gaining max HP also grants that much current HP; losing it only
            // lowers the ceiling.
            health.SetMax(newMax, Mathf.Max(0f, gained));
        }

        public float Get(StatType stat) => stats.Get(stat);
    }
}
