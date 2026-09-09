using System;
using UnityEngine;

namespace ScalePunch.Progression
{
    /// <summary>
    /// A dropped XP pickup. Scatters briefly, then flies to the player once
    /// inside the pickup radius — the flight is deliberately fast and
    /// accelerating, because collection is the reward and it should feel greedy.
    /// </summary>
    public class XPGem : MonoBehaviour
    {
        [SerializeField] float scatterSpeed = 3.5f;
        [SerializeField] float scatterDrag = 8f;
        [SerializeField] float attractAcceleration = 55f;
        [SerializeField] float maxAttractSpeed = 22f;
        [Tooltip("Distance at which the gem counts as collected.")]
        [SerializeField] float collectDistance = 0.5f;
        [Tooltip("Backstop so a gem dropped outside the arena can never linger forever.")]
        [SerializeField] float lifetime = 60f;

        public int Value { get; private set; }
        public Action<XPGem> Collected;
        public Action<XPGem> Expired;

        Transform _player;
        Vector3 _scatterVelocity;
        float _attractSpeed;
        float _lifeRemaining;
        bool _live;
        bool _attracting;

        public void Drop(Vector3 position, int value, Transform player)
        {
            transform.position = position;
            Value = value;
            _player = player;

            Vector2 random = UnityEngine.Random.insideUnitCircle.normalized;
            _scatterVelocity = new Vector3(random.x, 0f, random.y) * scatterSpeed;

            _attractSpeed = 0f;
            _lifeRemaining = lifetime;
            _attracting = false;
            _live = true;
        }

        /// <summary>Folds extra value in without disturbing the gem's flight.</summary>
        public void AddValue(int extra) => Value += Mathf.Max(0, extra);

        public void Update()
        {
            if (!_live || _player == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _lifeRemaining -= dt;
            if (_lifeRemaining <= 0f) { _live = false; Expired?.Invoke(this); return; }

            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            if (distance <= collectDistance)
            {
                _live = false;
                Collected?.Invoke(this);
                return;
            }

            if (_attracting)
            {
                // Accelerates the whole way in; a constant-speed gem reads as
                // floating rather than being pulled.
                _attractSpeed = Mathf.Min(maxAttractSpeed, _attractSpeed + attractAcceleration * dt);
                transform.position += toPlayer.normalized * (_attractSpeed * dt);
                return;
            }

            transform.position += _scatterVelocity * dt;
            _scatterVelocity = Vector3.MoveTowards(_scatterVelocity, Vector3.zero, scatterDrag * dt);
        }

        /// <summary>Latching, not per-frame: once a gem starts flying to the
        /// player it must never stall because the radius shrank.</summary>
        public void TryAttract(float pickupRadius)
        {
            if (_attracting || !_live || _player == null) return;

            Vector3 delta = _player.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= pickupRadius * pickupRadius) _attracting = true;
        }

        void OnDisable() => _live = false;
    }
}
