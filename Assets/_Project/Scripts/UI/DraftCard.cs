using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ScalePunch.Abilities;
using ScalePunch.Combat;

namespace ScalePunch.UI
{
    /// <summary>
    /// One card in the level-up draft.
    ///
    /// Shows six things, and each earns its place: icon, name, what it does,
    /// rarity, the level it would become, and — the one players actually read —
    /// the before → after value of the stat it changes.
    /// </summary>
    public class DraftCard : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] Image icon;
        [SerializeField] Image iconFrame;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text descriptionLabel;
        [SerializeField] TMP_Text rarityLabel;
        [SerializeField] TMP_Text levelLabel;

        [Header("Before -> after row")]
        [SerializeField] GameObject valueRow;
        [SerializeField] TMP_Text beforeLabel;
        [SerializeField] TMP_Text afterLabel;
        [SerializeField] TMP_Text arrowLabel;

        [Header("Rarity treatment")]
        [SerializeField] Image glow;
        [Tooltip("How far the rarity colour is blended into the card body. Full " +
                 "saturation makes the text unreadable.")]
        [Range(0f, 1f)] [SerializeField] float bodyTint = 0.22f;
        [SerializeField] Color improvedColour = new(0.35f, 0.95f, 0.4f);
        [SerializeField] Color neutralColour = new(0.85f, 0.87f, 0.9f);

        AbilityOffer _offer;
        Action<AbilityOffer> _onChosen;

        void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(AbilityOffer offer, StatSheet stats, Action<AbilityOffer> onChosen)
        {
            _offer = offer;
            _onChosen = onChosen;

            AbilityDefinition definition = offer.Definition;
            AbilityLevel data = definition.LevelData(offer.NextLevel);
            Color rarityColour = RarityTable.Colour(offer.Rarity);

            if (icon != null)
            {
                icon.sprite = definition.icon;
                icon.enabled = definition.icon != null;
            }
            if (iconFrame != null) iconFrame.color = rarityColour;

            if (nameLabel != null) nameLabel.text = definition.displayName;
            if (descriptionLabel != null)
                descriptionLabel.text = data != null ? data.description : string.Empty;

            if (rarityLabel != null)
            {
                rarityLabel.text = RarityTable.Label(offer.Rarity);
                rarityLabel.color = rarityColour;
            }

            // "NEW" reads far more strongly than "Lv 1": it tells the player at a
            // glance which cards widen the build and which deepen it.
            if (levelLabel != null)
                levelLabel.text = offer.IsNew ? "NEW" : $"Lv {offer.NextLevel}";

            if (background != null)
                background.color = Color.Lerp(new Color(0.13f, 0.14f, 0.17f), rarityColour, bodyTint);

            if (glow != null)
            {
                glow.enabled = RarityTable.Glows(offer.Rarity);
                glow.color = new Color(rarityColour.r, rarityColour.g, rarityColour.b, 0.55f);
            }

            BindValues(offer, stats);
        }

        void BindValues(AbilityOffer offer, StatSheet stats)
        {
            if (valueRow == null) return;

            AbilityPreview preview = AbilityPreview.Build(offer, stats);

            if (string.IsNullOrEmpty(preview.After))
            {
                valueRow.SetActive(false);
                return;
            }

            valueRow.SetActive(true);

            if (beforeLabel != null)
            {
                beforeLabel.text = preview.HasBefore ? preview.Before : "—";
                beforeLabel.color = neutralColour;
            }

            if (arrowLabel != null) arrowLabel.text = "→";

            if (afterLabel != null)
            {
                afterLabel.text = preview.After;
                // Green always: every card is an upgrade, and colouring the result
                // is what makes the row scan in under a second.
                afterLabel.color = improvedColour;
            }
        }

        void HandleClick() => _onChosen?.Invoke(_offer);
    }
}
