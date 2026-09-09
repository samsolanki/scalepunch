using UnityEngine;

namespace ScalePunch.CameraRig
{
    /// <summary>
    /// Fixed-angle top-down follow with a small lead in the direction of travel,
    /// so the player sees more of where they are going than where they have been.
    ///
    /// Put this on a parent rig and the Camera (with CameraShake) as its child —
    /// shake writes localPosition, so the two never fight.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;

        [Header("Framing")]
        [SerializeField] Vector3 offset = new(0f, 14f, -9f);
        [Tooltip("Seconds to close roughly 63% of the gap. Lower is snappier.")]
        [SerializeField] float smoothTime = 0.12f;

        [Header("Lead")]
        [Tooltip("Metres the camera pushes ahead of the player's movement.")]
        [SerializeField] float leadDistance = 1.8f;
        [SerializeField] float leadSmoothTime = 0.35f;

        Vector3 _velocity;
        Vector3 _lead;
        Vector3 _leadVelocity;
        Vector3 _lastTargetPosition;

        void Start()
        {
            if (target == null) return;

            _lastTargetPosition = target.position;
            transform.position = target.position + offset;
            transform.LookAt(target.position);
        }

        void LateUpdate()
        {
            if (target == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;   // hitstop: hold the frame rather than divide by zero

            Vector3 targetPosition = target.position;
            Vector3 motion = (targetPosition - _lastTargetPosition) / dt;
            _lastTargetPosition = targetPosition;

            motion.y = 0f;
            Vector3 desiredLead = Vector3.ClampMagnitude(motion.normalized * leadDistance, leadDistance);
            _lead = Vector3.SmoothDamp(_lead, desiredLead, ref _leadVelocity, leadSmoothTime);

            transform.position = Vector3.SmoothDamp(
                transform.position, targetPosition + _lead + offset, ref _velocity, smoothTime);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (newTarget != null) _lastTargetPosition = newTarget.position;
        }
    }
}
