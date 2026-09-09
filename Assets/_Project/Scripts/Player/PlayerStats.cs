using UnityEngine;
using ScalePunch.Combat;

namespace ScalePunch.Player
{
    /// <summary>
    /// Owns the player's StatSheet and keeps Health's maximum in sync with it,
    /// so a +MaxHP ability at M1 raises current HP too rather than silently
    /// doing nothing.
    ///
    /// The player is a fixed emplacement — it never moves — so this is the only
    /// thing standing between the stat sheet and the weapon.
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
            health.Init(stats.Get(StatType.MaxHP), stats.Get(StatType.Armor));
        }

        void OnDestroy() => stats.Changed -= SyncHealth;

        void SyncHealth()
        {
            SyncArmor();

            float newMax = stats.Get(StatType.MaxHP);
            float gained = newMax - health.Max;
            if (Mathf.Approximately(gained, 0f)) return;

            // Gaining max HP also grants that much current HP; losing it only
            // lowers the ceiling.
            health.SetMax(newMax, Mathf.Max(0f, gained));
        }

        /// <summary>
        /// Armour is a stat like any other, so gear and abilities that raise it
        /// have to reach Health. Without this it stays whatever it was at spawn
        /// and every +armour upgrade does nothing.
        /// </summary>
        void SyncArmor() => health.SetArmor(stats.Get(StatType.Armor));

        public float Get(StatType stat) => stats.Get(stat);
    }
}
