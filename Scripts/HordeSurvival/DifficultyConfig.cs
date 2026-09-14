using Kuantech.Core.Database;
using Kuantech.Core.Database.Attributes;
using Kuantech.HordeSurvival;
using UnityEngine;

namespace Kuantech.HordeBonkers
{
    /// <summary>
    /// The single balance sheet for run difficulty. Holds the level-1 wave shape plus a handful of growth
    /// coefficients; PowerLevel turns these into the concrete numbers a run uses. No inspector curves —
    /// just coefficients, so the whole game can be tuned from here (and, via the growth fields' database
    /// columns, from a remote sheet without a rebuild).
    ///
    /// Two layers, kept separate:
    ///   Layer 1 (across levels): PowerLevel → multipliers (budget/cap) + enemy level, computed once.
    ///   Layer 2 (within a level): wave index → per-wave numbers, using BaseConfig scaled by Layer 1.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Kuantech/Horde/Difficulty Config")]
    public class DifficultyConfig : ScriptableObject
    {
        [Header("Level-1 Wave Shape")]
        [Tooltip("The wave shape at power level 1. PowerLevel scales this up, it does not replace it.")]
        public HordeWaveConfig BaseConfig;

        [Header("Structure")]
        [Tooltip("Waves in a level before the boss/completion. Fixed for now.")]
        [KtDatabaseVariable("WavesPerLevel")]
        public int WavesPerLevel = 5;

        [Header("Growth per Power Level (linear: 1 + k * powerLevel)")]
        [Tooltip("How much the wave budget multiplier grows per power level.")]
        [KtDatabaseVariable("BudgetGrowthPerLevel")]
        public float BudgetGrowthPerLevel = 0.15f;

        [Tooltip("How much the concurrent cap multiplier grows per power level.")]
        [KtDatabaseVariable("CapGrowthPerLevel")]
        public float CapGrowthPerLevel = 0.1f;

        [Tooltip("Reward multiplier growth per power level.")]
        [KtDatabaseVariable("GoldGrowthPerLevel")]
        public float GoldGrowthPerLevel = 0.2f;

        [Header("Enemy Level")]
        [Tooltip("Enemy level = round(EnemyLevelBase + EnemyLevelPerPower * powerLevel).")]
        [KtDatabaseVariable("EnemyLevelBase")]
        public float EnemyLevelBase = 0f;
        [KtDatabaseVariable("EnemyLevelPerPower")]
        public float EnemyLevelPerPower = 1f;

        [Tooltip("The boss spawns this many levels above the regular enemies of the same power level.")]
        [KtDatabaseVariable("BossLevelBonus")]
        public int BossLevelBonus = 3;

        #region Layer 1 — power level → run values

        public float GetBudgetMultiplier(int powerLevel) => 1f + BudgetGrowthPerLevel * powerLevel;
        public float GetCapMultiplier(int powerLevel) => 1f + CapGrowthPerLevel * powerLevel;
        public float GetGoldMultiplier(int powerLevel) => 1f + GoldGrowthPerLevel * powerLevel;

        public int GetEnemyLevel(int powerLevel)
            => Mathf.Max(1, Mathf.RoundToInt(EnemyLevelBase + EnemyLevelPerPower * powerLevel));

        public int GetBossLevel(int powerLevel) => GetEnemyLevel(powerLevel) + BossLevelBonus;

        #endregion

        #region Layer 2 — wave index → per-wave numbers (BaseConfig scaled by Layer 1)

        /// <summary>Total threat budget for a wave: the base curve scaled by the power-level multiplier.</summary>
        public int GetWaveBudget(int waveIndex, int powerLevel)
        {
            float shape = BaseConfig.BudgetBase
                          + BaseConfig.BudgetLinear * waveIndex
                          + BaseConfig.BudgetQuad * waveIndex * waveIndex;
            return Mathf.Max(0, Mathf.RoundToInt(shape * GetBudgetMultiplier(powerLevel)));
        }

        /// <summary>Concurrent alive cap for a wave, scaled by power level and clamped by the hard CapMax.</summary>
        public int GetConcurrentCap(int waveIndex, int powerLevel)
        {
            float shape = BaseConfig.CapBase + BaseConfig.CapPerWave * waveIndex;
            int cap = Mathf.RoundToInt(shape * GetCapMultiplier(powerLevel));
            if (BaseConfig.CapMax > 0) cap = Mathf.Min(cap, BaseConfig.CapMax);
            return Mathf.Max(1, cap);
        }

        #endregion

        /// <summary>Pulls the growth coefficients from a one-row difficulty table (remote-tunable).</summary>
        public void UpdateFromDatabaseRow(DataTable.KtRowData rowData)
        {
            if (rowData == null) return;
            DataTable.SetVariablesFromRow(this, rowData);
        }

        /// <summary>
        /// Fills every field from an ArenaData row (see Arenas.json) -- the arena-driven counterpart of
        /// UpdateFromDatabaseRow. Used to build a fresh, run-owned instance per arena rather than mutating
        /// the single DifficultyConfig asset every Arena.prefab still shares.
        /// </summary>
        public void ApplyFromArenaData(ArenaData data)
        {
            if (data == null) return;

            BaseConfig.BudgetBase = data.BudgetBase;
            BaseConfig.BudgetLinear = data.BudgetLinear;
            BaseConfig.BudgetQuad = data.BudgetQuad;
            BaseConfig.CapBase = data.CapBase;
            BaseConfig.CapPerWave = data.CapPerWave;
            BaseConfig.CapMax = data.CapMax;
            BaseConfig.SpawnInterval = data.SpawnInterval;
            BaseConfig.MinBatchSize = data.MinBatchSize;
            BaseConfig.MaxBatchSize = data.MaxBatchSize;
            BaseConfig.BatchRadius = data.BatchRadius;

            WavesPerLevel = data.WavesPerLevel;
            BudgetGrowthPerLevel = data.BudgetGrowthPerLevel;
            CapGrowthPerLevel = data.CapGrowthPerLevel;
            GoldGrowthPerLevel = data.GoldGrowthPerLevel;
            EnemyLevelBase = data.EnemyLevelBase;
            EnemyLevelPerPower = data.EnemyLevelPerPower;
            BossLevelBonus = data.BossLevelBonus;
        }
    }
}
