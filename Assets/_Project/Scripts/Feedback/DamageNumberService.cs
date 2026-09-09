using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Core;
using ScalePunch.Data;

namespace ScalePunch.Feedback
{
    /// <summary>
    /// Pooled, hard-capped damage numbers on one screen-space canvas.
    /// Damage numbers are the classic frame killer in this genre
    /// (docs/02-tech-stack.md §5) — the cap is not optional.
    /// </summary>
    public class DamageNumberService : MonoSingleton<DamageNumberService>
    {
        [SerializeField] CombatTuning tuning;
        [SerializeField] DamageNumber prefab;
        [SerializeField] RectTransform canvasRoot;
        [SerializeField] Camera worldCamera;
        [SerializeField] int maxConcurrent = 40;

        Pool<DamageNumber> _pool;
        readonly List<DamageNumber> _live = new(64);

        protected override void Awake()
        {
            base.Awake();
            if (worldCamera == null) worldCamera = Camera.main;
            if (prefab != null && canvasRoot != null)
                _pool = new Pool<DamageNumber>(prefab, canvasRoot, maxConcurrent);
        }

        public void Show(float amount, bool isCrit, Vector3 worldPosition)
        {
            if (_pool == null || tuning == null) return;

            Recycle();

            // Over budget: drop the oldest rather than the newest, so the number
            // for the hit you just landed always survives.
            if (_live.Count >= maxConcurrent)
            {
                DamageNumber oldest = _live[0];
                _live.RemoveAt(0);
                oldest.gameObject.SetActive(false);
                _pool.Release(oldest);
            }

            DamageNumber number = _pool.Get(worldPosition, Quaternion.identity);
            number.transform.SetParent(canvasRoot, false);
            number.Play(amount, isCrit, worldPosition, worldCamera, tuning);
            _live.Add(number);
        }

        void Recycle()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                if (_live[i] == null) { _live.RemoveAt(i); continue; }
                if (!_live[i].IsFinished) continue;

                _pool.Release(_live[i]);
                _live.RemoveAt(i);
            }
        }
    }
}
