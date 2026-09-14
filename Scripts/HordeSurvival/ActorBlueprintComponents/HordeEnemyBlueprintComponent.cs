using System;
using Kuantech.Core;
using Kuantech.Core.Database;
using Kuantech.Core.Database.Attributes;
using UnityEngine;

namespace Kuantech.HordeBonkers
{
    /// <summary>
    /// Horde-specific spawn metadata attached to an enemy's blueprint: where in the difficulty curve it
    /// appears, how often it is picked, and how much of the wave budget it costs. Kept as a blueprint
    /// component (not baked into the generic ActorBlueprint) so Core never learns horde concepts, and
    /// pulled from the same database row as the enemy's stats so one sheet row defines the whole enemy.
    /// </summary>
    [Serializable]
    public class HordeEnemyBlueprintComponent : ActorBlueprintComponent
    {
        [Header("Power Level Gate (across levels)")]
        [Tooltip("Lowest power level at which this enemy can appear.")]
        [KtDatabaseVariable("MinPowerLevel")]
        public int MinPowerLevel = 1;

        [Tooltip("Highest power level at which this enemy still appears. 0 = no upper limit.")]
        [KtDatabaseVariable("MaxPowerLevel")]
        public int MaxPowerLevel = 0;

        [Header("Wave Gate (within a level)")]
        [Tooltip("Earliest wave (within a level) this enemy appears in. 0 = from the first wave. Use this " +
                 "to hold tougher enemies back from the opening waves.")]
        [KtDatabaseVariable("MinWave")]
        public int MinWave = 0;

        [Tooltip("Latest wave this enemy appears in. 0 = no upper limit.")]
        [KtDatabaseVariable("MaxWave")]
        public int MaxWave = 0;

        [Tooltip("Relative chance of being picked among eligible enemies (weighted random).")]
        [KtDatabaseVariable("SpawnWeight")]
        public float SpawnWeight = 1f;

        [Tooltip("How much of the wave budget spawning this enemy spends (a tank costs more than a grunt).")]
        [KtDatabaseVariable("SpawnBudget")]
        public int SpawnBudget = 1;

        [Tooltip("Distance the enemy tries to get in before attacking")]
        [KtDatabaseVariable("ApproachDistance")]
        public float ApproachDistance = 2f;

        [Tooltip("Seconds between this enemy's attacks.")]
        [KtDatabaseVariable("AttackCooldown")]
        public float AttackCooldown = 1.5f;

        [Tooltip("How long, in seconds, this enemy may sit off-screen — after having been seen on-screen " +
                 "at least once — before it asks to be moved to a fresh position.")]
        [KtDatabaseVariable("RelocationVisibilityTimeout")]
        public float RelocationVisibilityTimeout = 2f;

        [KtDatabaseVariable("ActorRadius")]
        public float ActorRadius = 1.0f;

        /// <summary>
        /// True if this enemy may spawn at the given power level AND wave. Both gates must pass: the power
        /// level decides whether the enemy is in this run's roster at all, the wave decides whether it has
        /// unlocked yet within the level (so tanks can be held out of the opening waves).
        /// </summary>
        public bool IsEligibleAt(int powerLevel, int waveIndex)
        {
            if (powerLevel < MinPowerLevel) return false;
            if (MaxPowerLevel > 0 && powerLevel > MaxPowerLevel) return false;
            if (waveIndex < MinWave) return false;
            if (MaxWave > 0 && waveIndex > MaxWave) return false;
            return true;
        }

        public override void OnActorCreated(ActorBlueprint blueprint, Actor actor)
        {
            HordeEnemyModule hem = actor.GetModule<HordeEnemyModule>();
            if(hem != null)
            {
                hem.ApproachDistance = ApproachDistance;
                hem.AttackCooldown = AttackCooldown;
                hem.RelocationVisibilityTimeout = RelocationVisibilityTimeout;
            }

            actor.SetActorRadius(ActorRadius);
        }

        public override void UpdateFromDatabaseRow(DataTable.KtRowData rowData)
        {
            // Simple single-cell int/float columns — the reflection helper fills them by column name.
            DataTable.SetVariablesFromRow(this, rowData);
        }
    }
}
