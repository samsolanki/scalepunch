using UnityEngine;

namespace ScalePunch.Core
{
    /// <summary>
    /// Switches whole systems off while the game is a prototype.
    ///
    /// Toggles rather than commented-out code, on purpose: everything below is
    /// written, reviewed and compiling. Commenting it out across fifty files
    /// would make re-enabling a merge exercise, and code that does not compile
    /// while it is disabled rots silently against every change made around it.
    ///
    /// **The core loop is not toggleable** — stationary auto-fire, zombies,
    /// kill-triggered drafts. That is the thing being prototyped; if it is not
    /// fun, nothing here changes the answer.
    ///
    /// Create via Assets ▸ Create ▸ ScalePunch ▸ Prototype Config.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Prototype Config", fileName = "PrototypeConfig")]
    public class PrototypeConfig : ScriptableObject
    {
        [Header("Run structure")]
        [Tooltip("Off: zombies arrive on a simple accelerating timer, forever. " +
                 "On: the authored 8-wave Stage_01 timeline.")]
        public bool waves;

        [Tooltip("Off: no boss. On: the two-phase boss after the last wave.")]
        public bool boss;

        [Tooltip("Off: dying restarts the run after a beat, with no UI. " +
                 "On: the victory/defeat screen with its reward summary.")]
        public bool runEndScreen;

        [Header("Progression")]
        [Tooltip("Off: every card is a plain card. On: five rarity tiers that " +
                 "scale the card's magnitude.")]
        public bool abilityRarity;

        [Tooltip("Off: nothing is banked and no profile is written to disk. " +
                 "On: coins persist between runs.")]
        public bool saveAndCurrency;

        [Tooltip("Off: 3 abilities of 3 levels each. On: the full 8-ability, " +
                 "5-level roster. Read by RunSceneBuilder when authoring assets, " +
                 "so changing it needs a rebuild — not a runtime toggle.")]
        public bool fullAbilityRoster;

        [Header("Debug")]
        [Tooltip("One console line per round fired and per impact. Invaluable while " +
                 "the weapon is under suspicion, noisy once it is not — at 3 shots a " +
                 "second it fills the console fast.")]
        public bool verboseWeaponLog = true;

        [Header("Presentation")]
        [Tooltip("Off: no floating health bars over zombies.")]
        public bool healthBars;

        static PrototypeConfig _active;

        /// <summary>
        /// The config in force. Falls back to a default instance with everything
        /// off, so a scene missing its GameBootstrap still runs the prototype
        /// rather than throwing.
        /// </summary>
        public static PrototypeConfig Active
        {
            get
            {
                if (_active == null) _active = CreateInstance<PrototypeConfig>();
                return _active;
            }
        }

        public static void SetActive(PrototypeConfig config)
        {
            if (config != null) _active = config;
        }
    }
}
