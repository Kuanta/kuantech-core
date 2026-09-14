using System;
using UnityEngine;

namespace Kuantech.HordeBonkers
{
    // Enemy roster + per-enemy spawn gating now live on the enemy blueprints themselves
    // (ActorBlueprintCollection + HordeEnemyBlueprintComponent), so this file only holds the wave shape.

    /// <summary>
    /// Per-wave difficulty and spawn rhythm settings. The formulas scale with the wave index,
    /// and the spawn rhythm (one-by-one vs clusters) is fully configured here.
    /// </summary>
    [Serializable]
    public struct HordeWaveConfig
    {
        [Header("Budget Curve: base + linear*w + quad*w²")]
        public float BudgetBase;
        public float BudgetLinear;
        public float BudgetQuad;

        [Header("Concurrent Cap: base + perWave*w (clamped by CapMax, 0 = unlimited)")]
        public int CapBase;
        public int CapPerWave;
        public int CapMax;

        [Header("Spawn Rhythm")]
        [Tooltip("Seconds between batches. Smaller = denser flow.")]
        public float SpawnInterval;
        [Tooltip("Minimum enemies per batch. 1 = one by one.")]
        public int MinBatchSize;
        [Tooltip("Maximum enemies per batch. >1 = clusters.")]
        public int MaxBatchSize;
        [Tooltip("How far a batch spreads in the world.")]
        public float BatchRadius;
    }
}