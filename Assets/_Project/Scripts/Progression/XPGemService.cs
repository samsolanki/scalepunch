using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Core;
using ScalePunch.Player;

namespace ScalePunch.Progression
{
    /// <summary>
    /// Pools XP gems and drives their magnet check centrally.
    ///
    /// The radius test lives here rather than in each gem so it can be a single
    /// pass over live gems per frame instead of every gem independently
    /// re-reading the player's stat sheet.
    /// </summary>
    public class XPGemService : MonoSingleton<XPGemService>
    {
        [SerializeField] XPGem prefab;
        [SerializeField] Transform player;
        [SerializeField] PlayerStats playerStats;
        [SerializeField] LevelSystem levelSystem;
        [SerializeField] int prewarm = 128;

        [Tooltip("Gems beyond this are merged into the oldest live gem rather than " +
                 "spawned. A boss wave can drop hundreds at once and the player " +
                 "cannot perceive the difference.")]
        [SerializeField] int maxConcurrent = 200;

        Pool<XPGem> _pool;
        Transform _root;
        readonly List<XPGem> _live = new(256);
        readonly HashSet<XPGem> _wired = new();

        protected override void Awake()
        {
            base.Awake();

            _root = new GameObject("~XPGemPool").transform;
            _root.SetParent(transform, false);

            if (prefab != null) _pool = new Pool<XPGem>(prefab, _root, prewarm);
        }

        public void Drop(Vector3 position, int value)
        {
            if (_pool == null || player == null || value <= 0) return;

            // In Kills mode LevelSystem counts kills directly, so a gem would
            // grant nothing. Spawning pickups that do not pay out teaches the
            // player the wrong model of where progress comes from.
            if (levelSystem != null && levelSystem.Source != ProgressSource.XPGems) return;

            if (_live.Count >= maxConcurrent)
            {
                // Fold the value into an existing gem so XP is never lost, even
                // though the drop itself is not rendered. Must not re-Drop it —
                // that would restart the scatter on a gem already flying in.
                _live[0].AddValue(value);
                return;
            }

            XPGem gem = _pool.Get(position, Quaternion.identity);

            if (_wired.Add(gem))
            {
                gem.Collected += OnCollected;
                gem.Expired += Reclaim;
            }

            gem.Drop(position, value, player);
            _live.Add(gem);
        }

        void Update()
        {
            if (playerStats == null) return;

            float radius = playerStats.Get(StatType.PickupRadius);
            for (int i = 0; i < _live.Count; i++) _live[i].TryAttract(radius);
        }

        void OnCollected(XPGem gem)
        {
            if (levelSystem != null) levelSystem.AddXP(gem.Value);
            Reclaim(gem);
        }

        void Reclaim(XPGem gem)
        {
            _live.Remove(gem);
            _pool.Release(gem);
        }
    }
}
