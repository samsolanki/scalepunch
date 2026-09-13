using UnityEngine;
using ScalePunch.Abilities;

namespace ScalePunch.UI
{
    /// <summary>
    /// The row of booster slots. Binds the player's active abilities into them
    /// and ticks their cooldowns.
    ///
    /// Rebinding happens on the Granted event rather than every frame: the set
    /// of owned abilities changes a handful of times per run, and walking it
    /// sixty times a second to discover that is wasted work.
    /// </summary>
    public class AbilitySlotBar : MonoBehaviour
    {
        [SerializeField] AbilitySystem abilities;
        [SerializeField] AbilitySlot[] slots;

        [Tooltip("Slots beyond this index show as locked. Meta progression unlocks " +
                 "them later; in the prototype every slot is open.")]
        [SerializeField] int unlockedSlots = 4;
        [SerializeField] string lockedText = "LOCKED";

        void OnEnable()
        {
            if (abilities != null) abilities.Granted += OnGranted;
            Rebind();
        }

        void OnDisable()
        {
            if (abilities != null) abilities.Granted -= OnGranted;
        }

        void OnGranted(AbilityInstance instance) => Rebind();

        void Rebind()
        {
            if (slots == null) return;

            int next = 0;

            if (abilities != null)
            {
                // Actives only. A passive has no cooldown and nothing to show, and
                // filling the bar with them would bury the one thing this row
                // exists to communicate.
                foreach (AbilityInstance instance in abilities.Owned)
                {
                    if (!instance.IsActive) continue;
                    if (next >= slots.Length || next >= unlockedSlots) break;

                    slots[next].Bind(instance);
                    next++;
                }
            }

            for (int i = next; i < slots.Length; i++)
            {
                if (i < unlockedSlots) slots[i].SetEmpty();
                else slots[i].SetLocked(lockedText);
            }
        }

        void Update()
        {
            if (slots == null) return;

            // Unscaled so the sweep keeps reading during hitstop, and so a slot
            // that fired just before a draft still shows its flash.
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < slots.Length; i++) slots[i].Tick(dt);
        }
    }
}
