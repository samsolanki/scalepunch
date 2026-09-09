using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.UI;

namespace ScalePunch.Player
{
    /// <summary>
    /// Joystick-driven movement. The player controls position and nothing else —
    /// attacking is automatic (docs/01-game-design.md §2). Resist every urge to
    /// add an attack button; it halves session length on mobile.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] CharacterController controller;
        [SerializeField] PlayerStats stats;
        [SerializeField] VirtualJoystick joystick;
        [SerializeField] Transform model;

        [Tooltip("Degrees per second the model turns toward the movement direction.")]
        [SerializeField] float turnSpeed = 720f;
        [SerializeField] float gravity = -20f;

        float _verticalVelocity;

        public Vector3 MoveDirection { get; private set; }
        public bool IsMoving => MoveDirection.sqrMagnitude > 0.001f;

        void Reset()
        {
            controller = GetComponent<CharacterController>();
            stats = GetComponent<PlayerStats>();
        }

        void Awake()
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (stats == null) stats = GetComponent<PlayerStats>();
        }

        void Update()
        {
            Vector2 input = joystick != null ? joystick.Value : Vector2.zero;
            MoveDirection = new Vector3(input.x, 0f, input.y);

            if (MoveDirection.sqrMagnitude > 1f) MoveDirection.Normalize();

            float speed = stats.Get(StatType.MoveSpeed);
            Vector3 velocity = MoveDirection * speed;

            // Keeps the controller pinned to the ground over slopes and steps.
            _verticalVelocity = controller.isGrounded
                ? -1f
                : _verticalVelocity + gravity * Time.deltaTime;
            velocity.y = _verticalVelocity;

            controller.Move(velocity * Time.deltaTime);

            if (model != null && IsMoving)
                model.rotation = Quaternion.RotateTowards(
                    model.rotation,
                    Quaternion.LookRotation(MoveDirection, Vector3.up),
                    turnSpeed * Time.deltaTime);
        }
    }
}
