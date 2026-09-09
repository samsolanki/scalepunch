using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Data;

namespace ScalePunch.Feedback
{
    /// <summary>
    /// Flashes every renderer white on hit. Uses a MaterialPropertyBlock so it
    /// does not instance a material per enemy — 150 material instances is a
    /// silent memory and batching disaster.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] CombatTuning tuning;
        [SerializeField] Renderer[] renderers;

        static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        readonly List<Color> _originals = new();
        MaterialPropertyBlock _block;
        float _remaining;

        void Reset() => renderers = GetComponentsInChildren<Renderer>();

        void Awake()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();

            _block = new MaterialPropertyBlock();

            foreach (Renderer r in renderers)
                _originals.Add(r.sharedMaterial != null && r.sharedMaterial.HasProperty(ColorId)
                    ? r.sharedMaterial.GetColor(ColorId)
                    : Color.white);
        }

        public void Flash()
        {
            _remaining = tuning != null ? tuning.flashDuration : 0.08f;
            Apply(tuning != null ? tuning.flashColour : Color.white);
        }

        void Update()
        {
            if (_remaining <= 0f) return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) Restore();
        }

        void OnEnable() => Restore();

        void Apply(Color colour)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].GetPropertyBlock(_block);
                _block.SetColor(ColorId, colour);
                renderers[i].SetPropertyBlock(_block);
            }
        }

        void Restore()
        {
            if (_block == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].GetPropertyBlock(_block);
                _block.SetColor(ColorId, i < _originals.Count ? _originals[i] : Color.white);
                renderers[i].SetPropertyBlock(_block);
            }
        }
    }
}
