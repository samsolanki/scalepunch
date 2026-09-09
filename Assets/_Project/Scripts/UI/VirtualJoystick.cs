using UnityEngine;
using UnityEngine.EventSystems;

namespace ScalePunch.UI
{
    /// <summary>
    /// Floating joystick: the stick appears wherever the thumb lands rather than
    /// at a fixed spot. Fixed joysticks force the player to look at their hand
    /// instead of the screen, and cost noticeably more deaths per run.
    ///
    /// Drive this from a full-screen (or left-half) transparent Image with
    /// Raycast Target on. Uses EventSystems, so it works with either input backend.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Visuals (optional)")]
        [SerializeField] RectTransform background;
        [SerializeField] RectTransform handle;

        [Header("Config")]
        [Tooltip("Pixels from centre at which the stick reads as fully deflected.")]
        [SerializeField] float radius = 110f;
        [SerializeField] bool hideWhenIdle = true;

        [Header("Editor")]
        [Tooltip("WASD / arrow keys drive the stick in the editor. Legacy input only.")]
#pragma warning disable CS0414 // read only under ENABLE_LEGACY_INPUT_MANAGER
        [SerializeField] bool keyboardFallback = true;
#pragma warning restore CS0414

        RectTransform _rect;
        Canvas _canvas;
        Camera _uiCamera;
        Vector2 _origin;
        Vector2 _value;
        bool _dragging;

        /// <summary>Deflection in the range [-1, 1] on each axis. Zero when idle.</summary>
        public Vector2 Value => _dragging ? _value : KeyboardValue();

        void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();

            // Screen Space - Overlay canvases must pass a null camera to the
            // rect utilities, otherwise every point lands in the wrong place.
            _uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            SetVisible(!hideWhenIdle);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _dragging = true;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rect, eventData.position, _uiCamera, out _origin);

            if (background != null) background.anchoredPosition = _origin;
            SetVisible(true);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rect, eventData.position, _uiCamera, out Vector2 local);

            Vector2 delta = local - _origin;
            _value = delta.magnitude > radius ? delta.normalized : delta / radius;

            if (handle != null) handle.anchoredPosition = _origin + _value * radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragging = false;
            _value = Vector2.zero;

            if (handle != null && background != null)
                handle.anchoredPosition = background.anchoredPosition;

            if (hideWhenIdle) SetVisible(false);
        }

        void SetVisible(bool visible)
        {
            if (background != null) background.gameObject.SetActive(visible);
            if (handle != null) handle.gameObject.SetActive(visible);
        }

        Vector2 KeyboardValue()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!keyboardFallback) return Vector2.zero;

            Vector2 keys = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            return keys.sqrMagnitude > 1f ? keys.normalized : keys;
#else
            return Vector2.zero;
#endif
        }
    }
}
