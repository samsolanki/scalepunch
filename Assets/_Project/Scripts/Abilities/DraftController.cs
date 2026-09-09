using System;
using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Core;
using ScalePunch.Player;
using ScalePunch.Progression;

namespace ScalePunch.Abilities
{
    /// <summary>
    /// Turns a level-up into a choice of three. This is the only interaction a
    /// run has (docs/01-game-design.md §2.1), so the offer rules matter more
    /// than they look.
    /// </summary>
    public class DraftController : MonoBehaviour
    {
        [SerializeField] AbilityLibrary library;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] LevelSystem levels;
        [SerializeField] PlayerStats stats;

        [Header("Offer")]
        [SerializeField] int cardsPerDraft = 3;
        [Tooltip("Within this many drafts, the offer is forced to include an active " +
                 "if the player holds none. A run with no active by draft 2 stalls.")]
        [SerializeField] int guaranteeActiveWithinDrafts = 2;
        [Tooltip("Each point of Luck adds this much weight to abilities the player " +
                 "does not yet own, biasing toward breadth.")]
        [SerializeField] float luckNoveltyWeight = 0.25f;

        /// <summary>Raised with the cards to show. The UI subscribes; this class
        /// never touches a Canvas.</summary>
        public event Action<IReadOnlyList<AbilityDefinition>> OfferReady;

        readonly List<AbilityDefinition> _candidates = new(32);
        readonly List<AbilityDefinition> _offer = new(4);
        readonly Queue<int> _pendingLevels = new();

        bool _drafting;
        int _draftsTaken;

        void OnEnable()
        {
            if (levels != null) levels.LevelledUp += OnLevelledUp;
        }

        void OnDisable()
        {
            if (levels != null) levels.LevelledUp -= OnLevelledUp;
        }

        void OnLevelledUp(int level)
        {
            // Queued, not shown immediately: one gem can carry the player through
            // two levels, and each owes them a separate draft.
            _pendingLevels.Enqueue(level);
            if (!_drafting) ShowNext();
        }

        void ShowNext()
        {
            if (_pendingLevels.Count == 0)
            {
                if (_drafting && TimeController.Exists) TimeController.Instance.PopPause();
                _drafting = false;
                return;
            }

            _pendingLevels.Dequeue();

            if (!_drafting)
            {
                _drafting = true;
                if (TimeController.Exists) TimeController.Instance.PushPause();
            }

            BuildOffer();
            OfferReady?.Invoke(_offer);
        }

        /// <summary>Called by the UI when the player picks a card.</summary>
        public void Choose(AbilityDefinition definition)
        {
            if (!_drafting) return;

            abilities.Grant(definition);
            _draftsTaken++;
            ShowNext();
        }

        void BuildOffer()
        {
            _offer.Clear();
            if (library == null || abilities == null) return;

            CollectCandidates();

            if (_candidates.Count == 0)
            {
                if (library.fallback != null) _offer.Add(library.fallback);
                return;
            }

            // A run with no active by the second draft has nothing happening in
            // it, so force one into the offer before filling the rest normally.
            if (_draftsTaken < guaranteeActiveWithinDrafts && !HoldsAnyActive())
            {
                AbilityDefinition active = TakeWeighted(AbilityKind.Active);
                if (active != null) _offer.Add(active);
            }

            while (_offer.Count < cardsPerDraft && _candidates.Count > 0)
            {
                AbilityDefinition pick = TakeWeighted(null);
                if (pick == null) break;
                _offer.Add(pick);
            }

            // Never show fewer than three cards — a short offer reads as a bug.
            while (_offer.Count < cardsPerDraft && library.fallback != null)
                _offer.Add(library.fallback);
        }

        void CollectCandidates()
        {
            _candidates.Clear();
            bool atCap = abilities.Owned.Count >= library.maxDistinctAbilities;

            foreach (AbilityDefinition definition in library.abilities)
            {
                if (definition == null || definition.isEvolution) continue;

                AbilityInstance owned = abilities.Get(definition);

                // Maxed abilities are never offered; nor are new ones once the
                // player is at their distinct-ability cap, which is what forces
                // a build instead of a collection.
                if (owned != null)
                {
                    if (!owned.IsMaxed) _candidates.Add(definition);
                }
                else if (!atCap)
                {
                    _candidates.Add(definition);
                }
            }
        }

        bool HoldsAnyActive()
        {
            foreach (AbilityInstance instance in abilities.Owned)
                if (instance.IsActive) return true;

            return false;
        }

        /// <summary>Weighted pick, removed from the candidate pool so one offer
        /// can never show the same ability twice.</summary>
        AbilityDefinition TakeWeighted(AbilityKind? kindFilter)
        {
            float total = 0f;
            float luck = stats != null ? stats.Get(StatType.Luck) : 0f;

            for (int i = 0; i < _candidates.Count; i++)
            {
                if (kindFilter.HasValue && _candidates[i].kind != kindFilter.Value) continue;
                total += WeightOf(_candidates[i], luck);
            }

            if (total <= 0f) return null;

            float roll = UnityEngine.Random.value * total;

            for (int i = 0; i < _candidates.Count; i++)
            {
                AbilityDefinition candidate = _candidates[i];
                if (kindFilter.HasValue && candidate.kind != kindFilter.Value) continue;

                roll -= WeightOf(candidate, luck);
                if (roll > 0f) continue;

                _candidates.RemoveAt(i);
                return candidate;
            }
            return null;
        }

        float WeightOf(AbilityDefinition definition, float luck)
        {
            float weight = Mathf.Max(0.0001f, definition.draftWeight);
            if (luck > 0f && !abilities.Has(definition)) weight += luck * luckNoveltyWeight;

            return weight;
        }
    }
}
