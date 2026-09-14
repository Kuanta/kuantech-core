using System;
using System.Collections.Generic;
using Kuantech.HordeBonkers;
using Kuantech.Midcore;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// One playable arena entry — a single, self-contained (arena layout + tier) configuration. Flat on
    /// purpose: an earlier design nested a per-tier RunData list inside one arena, which tangled "what arena
    /// layout is this" with "what difficulty/reward tier is this". Now each tier is its own top-level
    /// ArenaData row (e.g. "Arena_0", "Arena_1", "Arena_2"); ArenaGroupId + Tier are what let the UI cluster
    /// tiers under one arena header (see the arena-select screen's "one title, N tier buttons" layout)
    /// without the data itself nesting anything.
    ///
    /// Loaded via JsonDataManager (same pattern as Skills.json/Chests.json), which deserializes with
    /// Newtonsoft (see JsonData.cs) rather than JsonUtility -- Rewards/FirstClearRewards/WaveEvents are
    /// polymorphic (a Reward can be a CurrencyReward, an ItemReward, ...; a WaveEvent's Trigger/Action are
    /// each their own hierarchy), and Newtonsoft's TypeNameHandling resolves the concrete type from a
    /// "$type" property written straight in Arenas.json -- no hand-rolled type-string-plus-JSON-blob
    /// indirection, and no escaped nested JSON to author by hand.
    /// </summary>
    [Serializable]
    public class ArenaData
    {
        [Header("Identity")]
        public string ArenaId;         // unique per row, e.g. "Arena_0"
        public string ArenaGroupId;    // shared across a layout's tiers, e.g. "TestArena" -- UI groups by this
        public int Tier;               // tier index within the group (0-based)

        public string ArenaName;
        public string ArenaIconId;
        public string ArenaPrefabId;   // matches an Arena.ArenaId -- resolved via ArenaManager.GetArenaById

        [Header("Run")]
        public int PowerLevel;

        [Header("Difficulty — wave shape (was DifficultyConfig.BaseConfig)")]
        public float BudgetBase;
        public float BudgetLinear;
        public float BudgetQuad;
        public int CapBase;
        public int CapPerWave;
        public int CapMax;
        public float SpawnInterval;
        public int MinBatchSize;
        public int MaxBatchSize;
        public float BatchRadius;

        [Header("Difficulty — power-level growth (was DifficultyConfig)")]
        public int WavesPerLevel;
        public float BudgetGrowthPerLevel;
        public float CapGrowthPerLevel;
        public float GoldGrowthPerLevel;
        public float EnemyLevelBase;
        public float EnemyLevelPerPower;
        public int BossLevelBonus;

        [Header("Reward Data")]
        [Tooltip("Paid out on every clear of this tier.")]
        public List<Reward> Rewards = new();

        [Tooltip("Paid out once, on top of Rewards, the FIRST time this tier is cleared.")]
        public List<Reward> FirstClearRewards = new();

        [Header("Wave Events")]
        public List<WaveEvent> WaveEvents = new();
    }

    /// <summary>
    /// Top-level shape of Arenas.json -- a flat list, not a Dictionary (kept flat for the same reason it was
    /// under JsonUtility: campaign order comes from array order, and ArenaManager builds its own
    /// (ArenaGroupId -> Tier -> ArenaData) lookup from this at load time).
    /// </summary>
    [Serializable]
    public class ArenaDataCollection
    {
        public List<ArenaData> Arenas;
    }
}
