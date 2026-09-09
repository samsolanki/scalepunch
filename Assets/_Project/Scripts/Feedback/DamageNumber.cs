using TMPro;
using UnityEngine;
using ScalePunch.Data;

namespace ScalePunch.Feedback
{
    /// <summary>One pooled floating number. Screen-space, projected from a world
    /// anchor each frame so it tracks the camera without a world canvas.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] TMP_Text label;

        RectTransform _rect;
        CanvasGroup _group;
        Camera _camera;
        CombatTuning _tuning;

        Vector3 _worldAnchor;
        Vector2 _screenOffset;
        Vector2 _drift;
        float _life;
        float _elapsed;

        public bool IsFinished { get; private set; } = true;

        void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            if (label == null) label = GetComponentInChildren<TMP_Text>();
        }

        public void Play(float amount, bool isCrit, Vector3 worldPosition, Camera cam, CombatTuning tuning)
        {
            _camera = cam;
            _tuning = tuning;
            _worldAnchor = worldPosition;
            _elapsed = 0f;
            _life = tuning.numberLifetime;
            IsFinished = false;

            _screenOffset = Vector2.zero;
            _drift = new Vector2(Random.Range(-tuning.numberSpread, tuning.numberSpread), 0f);

            label.text = Mathf.RoundToInt(amount).ToString();
            label.color = isCrit ? tuning.critColour : tuning.normalColour;
            _rect.localScale = Vector3.one * (isCrit ? tuning.critScale : 1f);
            _group.alpha = 1f;
        }

        void Update()
        {
            if (IsFinished) return;

            // Unscaled: numbers should keep reading during hitstop.
            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;

            float t = _elapsed / _life;
            if (t >= 1f)
            {
                IsFinished = true;
                gameObject.SetActive(false);
                return;
            }

            // Rises fast then slows — a pop, not a constant float upward.
            float riseSpeed = _tuning.numberRise * (1f - t * 0.6f);
            _screenOffset += new Vector2(_drift.x, riseSpeed) * dt;

            if (_camera != null)
                _rect.position = (Vector2)_camera.WorldToScreenPoint(_worldAnchor) + _screenOffset;

            _group.alpha = 1f - t * t;
        }
    }
}
