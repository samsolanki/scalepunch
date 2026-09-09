using UnityEngine;
using UnityEngine.UI;

namespace ScalePunch.UI
{
    /// <summary>
    /// One pooled floating bar. Holds no reference to what it is drawing - the
    /// service assigns it a target each frame - so it can be handed from a dying
    /// zombie to a fresh one with no reset logic.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class HealthBarWidget : MonoBehaviour
    {
        [SerializeField] RectTransform root;
        [SerializeField] Image background;
        [SerializeField] Image fill;
        [SerializeField] CanvasGroup group;

        [Tooltip("Seconds for the fill to catch up. A bar that snaps reads as a " +
                 "number changing; one that chases reads as damage.")]
        [SerializeField] float drainSpeed = 4f;

        float _shown = 1f;

        void Awake()
        {
            if (root == null) root = (RectTransform)transform;
            if (group == null) group = GetComponent<CanvasGroup>();
        }

        public void Show(Vector2 screenPosition, float normalised, float width, float alpha)
        {
            root.position = screenPosition;
            root.sizeDelta = new Vector2(width, root.sizeDelta.y);

            // Unscaled: bars must keep draining during hitstop, which is exactly
            // when the player is looking at them.
            _shown = Mathf.MoveTowards(_shown, normalised, drainSpeed * Time.unscaledDeltaTime);

            if (fill != null) fill.fillAmount = _shown;
            if (group != null) group.alpha = alpha;
        }

        /// <summary>Called when the pool hands this widget to a different target,
        /// so the fill does not visibly slide over from the previous one.</summary>
        public void Snap(float normalised)
        {
            _shown = normalised;
            if (fill != null) fill.fillAmount = normalised;
        }

        public void SetColour(Color fillColour, Color backColour)
        {
            if (fill != null) fill.color = fillColour;
            if (background != null) background.color = backColour;
        }
    }
}
