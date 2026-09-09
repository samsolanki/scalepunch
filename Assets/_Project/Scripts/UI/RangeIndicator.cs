using UnityEngine;
using ScalePunch.Player;

namespace ScalePunch.UI
{
    /// <summary>
    /// Draws the engagement radius on the ground as a LineRenderer ring.
    ///
    /// This is not decoration. The radius is the entire read of the game — the
    /// player must be able to see, at a glance, exactly where zombies start
    /// dying, or every range upgrade they buy is invisible to them.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RangeIndicator : MonoBehaviour
    {
        [SerializeField] AutoShoot weapon;
        [SerializeField] LineRenderer line;

        [Header("Shape")]
        [SerializeField] int segments = 72;
        [Tooltip("Lift above the ground plane, to avoid z-fighting with the floor.")]
        [SerializeField] float groundOffset = 0.03f;

        [Header("Pulse")]
        [Tooltip("Subtle breathing so the ring reads as active rather than as a decal. " +
                 "Off by default: any non-zero value defeats the rebuild-skip below and " +
                 "costs 72 SetPosition calls every frame. Cheap, but not free — turn it " +
                 "on only if the static ring looks dead on device.")]
        [SerializeField] float pulseAmplitude = 0f;
        [SerializeField] float pulseSpeed = 1.6f;

        float _lastRadius = -1f;

        void Reset()
        {
            line = GetComponent<LineRenderer>();
            weapon = GetComponentInParent<AutoShoot>();
        }

        void Awake()
        {
            if (line == null) line = GetComponent<LineRenderer>();
            if (weapon == null) weapon = GetComponentInParent<AutoShoot>();

            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = segments;
        }

        void LateUpdate()
        {
            if (weapon == null) return;

            float radius = weapon.Range;
            if (pulseAmplitude > 0f)
                radius += Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;

            // The radius only changes on an upgrade, so rebuilding the ring every
            // frame is wasted work — skip unless the value actually moved.
            if (Mathf.Approximately(radius, _lastRadius)) return;
            _lastRadius = radius;

            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(
                    Mathf.Sin(angle) * radius, groundOffset, Mathf.Cos(angle) * radius));
            }
        }
    }
}
