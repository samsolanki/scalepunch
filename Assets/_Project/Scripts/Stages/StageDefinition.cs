using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Enemies;

namespace ScalePunch.Stages
{
    /// <summary>
    /// One stage: a sequence of waves, then a boss, then the run is over.
    ///
    /// Difficulty across the campaign comes from the multipliers here, not from
    /// new mechanics per stage — which is what makes content cheap to produce
    /// once the systems exist (docs/01-game-design.md §6).
    ///
    /// Create via Assets ▸ Create ▸ ScalePunch ▸ Stage Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "ScalePunch/Stage Definition", fileName = "Stage_")]
    public class StageDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "stage_01";
        public string displayName = "Stage 1";
        [Tooltip("1-based position in the campaign. Recorded as highestStageCleared " +
                 "on a win, so it must be unique and ordered across stages.")]
        public int stageNumber = 1;

        [Header("Content")]
        public WaveDefinition[] waves = new WaveDefinition[0];
        [Tooltip("Spawned alone once every wave is done. Killing it wins the run. " +
                 "Leave empty for an endless or survival stage.")]
        public EnemyDefinition boss;
        [Tooltip("Seconds of quiet between the last wave and the boss. The pause is " +
                 "the telegraph — without it the boss reads as just another zombie.")]
        public float bossIntroSeconds = 2.5f;

        [Header("Scaling")]
        public float hpMultiplier = 1f;
        public float damageMultiplier = 1f;

        [Header("Rewards")]
        public int coinsOnClear = 250;
        public int coinsPerKill = 2;
        [Tooltip("Fraction of the clear bonus paid when the player dies. Never zero — " +
                 "sending someone away with nothing after a four-minute run is how " +
                 "you lose them (docs/01-game-design.md §6).")]
        [Range(0f, 1f)] public float failPayoutFraction = 0.35f;

        public int WaveCount => waves != null ? waves.Length : 0;

        public IReadOnlyCollection<EnemyDefinition> AllEnemies()
        {
            var set = new HashSet<EnemyDefinition>();

            if (waves != null)
                foreach (WaveDefinition wave in waves)
                    if (wave != null) wave.CollectEnemies(set);

            if (boss != null) set.Add(boss);
            return set;
        }

        /// <summary>Nominal stage length, for pacing checks. Excludes the boss fight,
        /// which is as long as it is.</summary>
        public float TotalWaveSeconds()
        {
            float total = 0f;
            if (waves == null) return total;

            foreach (WaveDefinition wave in waves)
                if (wave != null) total += wave.duration;

            return total;
        }
    }
}
