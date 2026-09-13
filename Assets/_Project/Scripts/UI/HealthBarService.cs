using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Core;

namespace ScalePunch.UI
{
    /// <summary>
    /// Draws floating health bars for every <see cref="HealthBarTarget"/> -
    /// zombies and the player alike - from one pool on one screen-space canvas.
    ///
    /// Deliberately NOT a world-space Canvas per enemy. At the 150-zombie budget
    /// that would mean 150 canvases, each its own rebuild and draw call, and it
    /// is one of the reliable ways to take a mobile survivors-like from 60 fps to
    /// 25. Projecting world positions onto one canvas costs a matrix multiply per
    /// visible bar instead.
    /// </summary>
    public class HealthBarService : MonoSingleton<HealthBarService>
    {
        [SerializeField] HealthBarWidget prefab;
        [SerializeField] RectTransform canvasRoot;
        [SerializeField] Camera worldCamera;

        [Header("Budget")]
        [Tooltip("Bars on screen at once. Beyond this, the most hurt and highest " +
                 "priority targets win - nobody can read forty bars anyway.")]
        [SerializeField] int maxConcurrent = 24;

        [Header("Fade")]
        [Tooltip("Bars shrink to nothing past this distance so a far-off crowd " +
                 "does not turn into a wall of pixels.")]
        [SerializeField] float fadeStartDistance = 18f;
        [SerializeField] float fadeEndDistance = 26f;

        [Header("Colour")]
        [SerializeField] Color healthy = new(0.35f, 0.85f, 0.4f);
        [SerializeField] Color hurt = new(0.95f, 0.75f, 0.2f);
        [SerializeField] Color critical = new(0.9f, 0.25f, 0.25f);
        [SerializeField] Color background = new(0.05f, 0.06f, 0.08f, 0.85f);

        Pool<HealthBarWidget> _pool;
        Transform _poolRoot;

        readonly List<HealthBarTarget> _visible = new(64);
        readonly Dictionary<HealthBarTarget, HealthBarWidget> _assigned = new();
        readonly List<HealthBarTarget> _expired = new(32);

        protected override void Awake()
        {
            base.Awake();

            if (!Core.PrototypeConfig.Active.healthBars)
            {
                enabled = false;
                return;
            }


            if (worldCamera == null) worldCamera = Camera.main;

            _poolRoot = new GameObject("~HealthBarPool").transform;
            _poolRoot.SetParent(transform, false);

            if (prefab != null && canvasRoot != null)
                _pool = new Pool<HealthBarWidget>(prefab, canvasRoot, maxConcurrent);
        }

        protected override void OnDestroy()
        {
            HealthBarTarget.Clear();
            base.OnDestroy();
        }

        void LateUpdate()
        {
            if (_pool == null || worldCamera == null) return;

            CollectVisible();
            ReleaseUnassigned();
            DrawVisible();
        }

        /// <summary>Everything hurt, in front of the camera, and close enough to
        /// read - ordered so the most urgent survive the budget cut.</summary>
        void CollectVisible()
        {
            _visible.Clear();

            IReadOnlyList<HealthBarTarget> all = HealthBarTarget.All;
            Vector3 eye = worldCamera.transform.position;

            for (int i = 0; i < all.Count; i++)
            {
                HealthBarTarget target = all[i];
                if (target == null || !target.WantsBar) continue;

                float distance = Vector3.Distance(eye, target.transform.position);
                if (distance > fadeEndDistance) continue;

                _visible.Add(target);
            }

            _visible.Sort(Compare);

            if (_visible.Count > maxConcurrent) _visible.RemoveRange(maxConcurrent, _visible.Count - maxConcurrent);
        }

        static int Compare(HealthBarTarget a, HealthBarTarget b)
        {
            // Explicit priority first (the player outranks every zombie), then
            // whoever is closest to dying.
            int byPriority = b.Priority.CompareTo(a.Priority);
            if (byPriority != 0) return byPriority;

            return a.Health.Normalised.CompareTo(b.Health.Normalised);
        }

        /// <summary>Reclaims widgets from targets that no longer qualify - healed,
        /// dead, despawned, or pushed out by the budget.</summary>
        void ReleaseUnassigned()
        {
            _expired.Clear();

            foreach (KeyValuePair<HealthBarTarget, HealthBarWidget> entry in _assigned)
            {
                if (entry.Key != null && _visible.Contains(entry.Key)) continue;
                _expired.Add(entry.Key);
            }

            for (int i = 0; i < _expired.Count; i++)
            {
                HealthBarTarget target = _expired[i];
                if (_assigned.TryGetValue(target, out HealthBarWidget widget)) _pool.Release(widget);

                _assigned.Remove(target);
            }
        }

        void DrawVisible()
        {
            Vector3 eye = worldCamera.transform.position;

            for (int i = 0; i < _visible.Count; i++)
            {
                HealthBarTarget target = _visible[i];
                Vector3 world = target.transform.position + Vector3.up * target.HeightOffset;
                Vector3 screen = worldCamera.WorldToScreenPoint(world);

                // Behind the camera projects to a mirrored on-screen point.
                if (screen.z <= 0f) continue;

                if (!_assigned.TryGetValue(target, out HealthBarWidget widget))
                {
                    widget = _pool.Get(Vector3.zero, Quaternion.identity);
                    widget.transform.SetParent(canvasRoot, false);
                    widget.Snap(target.Health.Normalised);   // no slide from the previous owner
                    _assigned[target] = widget;
                }

                float normalised = target.Health.Normalised;
                widget.SetColour(ColourFor(normalised), background);

                float distance = Vector3.Distance(eye, target.transform.position);
                float alpha = fadeEndDistance <= fadeStartDistance
                    ? 1f
                    : 1f - Mathf.Clamp01((distance - fadeStartDistance) / (fadeEndDistance - fadeStartDistance));

                widget.Show(screen, normalised, target.Width, alpha);
            }
        }

        Color ColourFor(float normalised)
        {
            if (normalised > 0.6f) return healthy;
            return normalised > 0.3f ? hurt : critical;
        }
    }
}
