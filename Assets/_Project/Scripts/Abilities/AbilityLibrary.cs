using System.Collections.Generic;
using UnityEngine;

namespace ScalePunch.Abilities
{
    /// <summary>
    /// Every ability that can appear in a draft.
    ///
    /// Create via Assets ▸ Create ▸ ScalePunch ▸ Ability Library.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Ability Library", fileName = "AbilityLibrary")]
    public class AbilityLibrary : ScriptableObject
    {
        public List<AbilityDefinition> abilities = new();

        [Tooltip("Offered when nothing else is draftable — everything owned is maxed. " +
                 "Should be an infinitely repeatable filler such as a small heal.")]
        public AbilityDefinition fallback;

        [Tooltip("Cap on distinct abilities one run may hold. Without a cap the " +
                 "player ends every run with everything and no build identity.")]
        public int maxDistinctAbilities = 8;
    }
}
