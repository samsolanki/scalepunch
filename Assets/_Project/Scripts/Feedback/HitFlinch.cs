using UnityEngine;
using ScalePunch.Combat;

namespace ScalePunch.Feedback
{
    /// <summary>
    /// Rocks the body back along the line of fire when hit, then recovers.
    ///
    /// Positional knockback alone slides a capsule across the floor, which reads
    /// as ice rather than impact. The lean is what sells a bullet landing - and
    /// it works on targets that resist knockback entirely, so a Brute still
    /// visibly reacts to being shot without being pushed around.
    /// </summary>
    public class HitFlinch : MonoBehaviour
    {
        [SerializeField] Health health;
        [Tooltip("The visual to rock. Never the root - the root is what aims and moves.")]
        [SerializeField] Transform body;

        [Header("Feel")]
        [SerializeField] float tiltDegrees = 24f;
        [SerializeField] float critTiltDegrees = 38f;
        [Tooltip("How fast the lean unwinds. Higher recovers sooner.")]
        [SerializeField] float recoverSpeed = 7f;

        Quaternion _rest;
        Vector3 _axis = Vector3.right;
        float _amount;
        float _degrees;

        void Reset() => health = GetComponent<Health>();

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (body == null) body = transform;

            _rest = body.localRotation;
            health.Damaged += OnDamaged;
            health.Died += OnDamaged;
        }

        void OnDestroy()
        {
            if (health == null) return;

            health.Damaged -= OnDamaged;
            health.Died -= OnDamaged;
        }

        void OnEnable()
        {
            // Pooled objects come back mid-lean otherwise.
            _amount = 0f;
            body.localRotation = _rest;
        }

        void OnDamaged(DamageInfo info)
        {
            Vector3 push = transform.position - info.Origin;
            push.y = 0f;

            // A shot from directly overhead has no direction to lean away from;
            // fall back on rocking straight backwards.
            Vector3 local = push.sqrMagnitude < 0.0001f
                ? Vector3.back
                : transform.InverseTransformDirection(push.normalized);

            // Rotating about (z, 0, -x) tips the top of the body along the push
            // direction, i.e. away from whoever fired.
            _axis = new Vector3(local.z, 0f, -local.x);
            _degrees = info.IsCrit ? critTiltDegrees : tiltDegrees;
            _amount = 1f;
        }

        void Update()
        {
            if (_amount <= 0f) return;

            // Scaled time on purpose: the lean holds through the hitstop its own
            // impact caused, which is the frame the hit is read on.
            _amount = Mathf.MoveTowards(_amount, 0f, recoverSpeed * Time.deltaTime);
            body.localRotation = _rest * Quaternion.AngleAxis(_degrees * _amount, _axis);
        }
    }
}
