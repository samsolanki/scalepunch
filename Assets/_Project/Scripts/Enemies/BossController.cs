using System;
using UnityEngine;
using ScalePunch.Combat;

namespace ScalePunch.Enemies
{
    /// <summary>
    /// Two-phase boss. Walks in like any zombie, then periodically winds up and
    /// charges the player.
    ///
    /// The charge exists because a stationary player cannot dodge — so the threat
    /// has to be something they can *answer* rather than avoid: burn it down
    /// during the telegraph, or eat the hit. That is the only real decision a
    /// boss can offer in this design, which is why the wind-up is long and
    /// unmistakable.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    [RequireComponent(typeof(EnemyMovement))]
    public class BossController : MonoBehaviour
    {
        enum State { Approach, Telegraph, Charging, Recover }

        [SerializeField] Enemy enemy;
        [SerializeField] EnemyMovement movement;
        [SerializeField] Health health;
        [Tooltip("The visual that scales during the wind-up. The root, never scaled, " +
                 "keeps driving position.")]
        [SerializeField] Transform body;

        [Header("Phase two")]
        [Tooltip("Fraction of max HP at which the boss enrages.")]
        [Range(0.05f, 0.95f)] [SerializeField] float phaseTwoAt = 0.5f;
        [SerializeField] float phaseTwoSpeedMultiplier = 1.45f;
        [SerializeField] float phaseTwoDamageMultiplier = 1.35f;
        [Tooltip("Charges come this much sooner in phase two.")]
        [Range(0.2f, 1f)] [SerializeField] float phaseTwoChargeIntervalScale = 0.6f;

        [Header("Charge")]
        [SerializeField] float chargeInterval = 8f;
        [Tooltip("Wind-up length. Long on purpose — it is the player's window to " +
                 "react, and a short telegraph reads as an unfair hit.")]
        [SerializeField] float telegraphSeconds = 1.2f;
        [SerializeField] float chargeSpeed = 15f;
        [SerializeField] float chargeSeconds = 0.75f;
        [SerializeField] float chargeDamage = 34f;
        [Tooltip("How close the charge must pass to connect.")]
        [SerializeField] float chargeHitRadius = 1.6f;
        [SerializeField] float recoverSeconds = 0.9f;

        [Header("Telegraph look")]
        [SerializeField] float telegraphScale = 1.25f;
        [SerializeField] float telegraphPulseSpeed = 14f;

        State _state = State.Approach;
        float _timer;
        float _stateTimer;
        bool _phaseTwo;
        bool _chargeConnected;
        Vector3 _chargeDirection;
        Vector3 _bodyBaseScale;

        /// <summary>Raised when the boss enters phase two, for UI and audio.</summary>
        public event Action Enraged;
        /// <summary>Raised at the start of a wind-up, so a telegraph VFX can play.</summary>
        public event Action TelegraphStarted;

        public bool IsPhaseTwo => _phaseTwo;

        void Reset()
        {
            enemy = GetComponent<Enemy>();
            movement = GetComponent<EnemyMovement>();
            health = GetComponent<Health>();
        }

        void Awake()
        {
            if (enemy == null) enemy = GetComponent<Enemy>();
            if (movement == null) movement = GetComponent<EnemyMovement>();
            if (health == null) health = GetComponent<Health>();
            if (body == null && transform.childCount > 0) body = transform.GetChild(0);

            if (body != null) _bodyBaseScale = body.localScale;
        }

        void OnEnable()
        {
            // Pooled: every field that describes "where we are in the fight" has
            // to be reset here, or a reused boss starts mid-charge and enraged.
            _state = State.Approach;
            _timer = chargeInterval;
            _stateTimer = 0f;
            _phaseTwo = false;
            _chargeConnected = false;

            if (movement != null) movement.ExternalControl = false;
            if (body != null) body.localScale = _bodyBaseScale;
        }

        void OnDisable()
        {
            if (movement != null) movement.ExternalControl = false;
            if (body != null) body.localScale = _bodyBaseScale;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || health == null || health.IsDead) return;

            CheckPhase();

            switch (_state)
            {
                case State.Approach: TickApproach(dt); break;
                case State.Telegraph: TickTelegraph(dt); break;
                case State.Charging: TickCharge(dt); break;
                case State.Recover: TickRecover(dt); break;
            }
        }

        void CheckPhase()
        {
            if (_phaseTwo || health.Normalised > phaseTwoAt) return;

            _phaseTwo = true;
            movement.SetSpeedAndDamage(phaseTwoSpeedMultiplier, phaseTwoDamageMultiplier);
            Enraged?.Invoke();
        }

        void TickApproach(float dt)
        {
            float interval = _phaseTwo ? chargeInterval * phaseTwoChargeIntervalScale : chargeInterval;

            _timer -= dt;
            if (_timer > 0f) return;

            if (movement.Target == null) return;

            _state = State.Telegraph;
            _stateTimer = telegraphSeconds;
            _timer = interval;

            // Stops dead during the wind-up. A boss that keeps walking while
            // telegraphing gives the player two things to read at once.
            movement.ExternalControl = true;
            TelegraphStarted?.Invoke();
        }

        void TickTelegraph(float dt)
        {
            _stateTimer -= dt;

            // Keeps facing the player right up to the commit, so the charge
            // direction is never a surprise.
            FaceTarget();

            if (body != null)
            {
                float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.time * telegraphPulseSpeed)) * (telegraphScale - 1f);
                body.localScale = _bodyBaseScale * pulse;
            }

            if (_stateTimer > 0f) return;

            // Direction is locked at the moment of commit, not tracked during the
            // charge — a homing charge is undodgeable and reads as cheating.
            _chargeDirection = AimDirection();
            _chargeConnected = false;
            _state = State.Charging;
            _stateTimer = chargeSeconds;

            if (body != null) body.localScale = _bodyBaseScale;
        }

        void TickCharge(float dt)
        {
            _stateTimer -= dt;
            transform.position += _chargeDirection * (chargeSpeed * dt);

            if (!_chargeConnected && movement.Target != null)
            {
                Vector3 delta = movement.Target.position - transform.position;
                delta.y = 0f;

                if (delta.sqrMagnitude <= chargeHitRadius * chargeHitRadius)
                {
                    // Once per charge. Without the latch the player takes a hit
                    // every frame the boss overlaps them, which is instant death.
                    _chargeConnected = movement.TryDamageTarget(chargeDamage);
                }
            }

            if (_stateTimer > 0f) return;

            _state = State.Recover;
            _stateTimer = recoverSeconds;
        }

        void TickRecover(float dt)
        {
            _stateTimer -= dt;
            if (_stateTimer > 0f) return;

            _state = State.Approach;
            movement.ExternalControl = false;
        }

        Vector3 AimDirection()
        {
            if (movement.Target == null) return transform.forward;

            Vector3 delta = movement.Target.position - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude < 0.0001f ? transform.forward : delta.normalized;
        }

        void FaceTarget()
        {
            Vector3 direction = AimDirection();
            if (direction.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
