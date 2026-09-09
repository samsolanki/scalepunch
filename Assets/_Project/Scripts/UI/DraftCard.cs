using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ScalePunch.Abilities;

namespace ScalePunch.UI
{
    /// <summary>One card in the level-up draft.</summary>
    public class DraftCard : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] TMP_Text descriptionLabel;
        [SerializeField] Image kindStripe;

        [Header("Kind colours")]
        [SerializeField] Color activeColour = new(0.95f, 0.45f, 0.2f);
        [SerializeField] Color passiveColour = new(0.3f, 0.65f, 0.95f);

        AbilityDefinition _definition;
        Action<AbilityDefinition> _onChosen;

        void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        void OnDestroy() => button.onClick.RemoveListener(HandleClick);

        public void Bind(AbilityDefinition definition, int currentLevel, Action<AbilityDefinition> onChosen)
        {
            _definition = definition;
            _onChosen = onChosen;

            int nextLevel = currentLevel + 1;
            AbilityLevel data = definition.LevelData(nextLevel);

            if (icon != null)
            {
                icon.sprite = definition.icon;
                icon.enabled = definition.icon != null;
            }

            if (nameLabel != null) nameLabel.text = definition.displayName;

            // "NEW" is a much stronger read than "Lv 1" — it tells the player at a
            // glance which cards widen the build and which deepen it.
            if (levelLabel != null)
                levelLabel.text = currentLevel == 0 ? "NEW" : $"Lv {nextLevel}";

            if (descriptionLabel != null)
                descriptionLabel.text = data != null ? data.description : string.Empty;

            if (kindStripe != null)
                kindStripe.color = definition.kind == AbilityKind.Active ? activeColour : passiveColour;
        }

        void HandleClick() => _onChosen?.Invoke(_definition);
    }
}
