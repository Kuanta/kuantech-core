using System;
using UnityEngine;

namespace Kuantech.Analytics
{
    public sealed class LevelStartedEventArgs : EventArgs
    {
        public int LevelIndex { get; }
        public LevelStartedEventArgs(int levelIndex) => LevelIndex = levelIndex;
    }

    public sealed class LevelEndedEventArgs : EventArgs
    {
        public int LevelIndex { get; }
        public bool IsCompleted { get; }
        public float Score { get; }
        public LevelEndedEventArgs(int levelIndex, bool isCompleted, float score)
        {
            LevelIndex = levelIndex; IsCompleted = isCompleted; Score = score;
        }
    }

    public sealed class WorldLevelStartedEventArgs : EventArgs
    {
        public int WorldIndex { get; }
        public int LevelIndex { get; }
        public WorldLevelStartedEventArgs(int worldIndex, int levelIndex)
        { WorldIndex = worldIndex; LevelIndex = levelIndex; }
    }

    public sealed class WorldLevelEndedEventArgs : EventArgs
    {
        public int WorldIndex { get; }
        public int LevelIndex { get; }
        public bool IsCompleted { get; }
        public float Score { get; }
        public WorldLevelEndedEventArgs(int worldIndex, int levelIndex, bool isCompleted, float score)
        {
            WorldIndex = worldIndex; LevelIndex = levelIndex; IsCompleted = isCompleted; Score = score;
        }
    }

    public sealed class UpgradePurchasedEventArgs : EventArgs
    {
        public string UpgradeId { get; }
        public int Level { get; }
        public UpgradePurchasedEventArgs(string upgradeId, int level)
        { UpgradeId = upgradeId; Level = level; }
    }

    public sealed class UpgradeWithCategoryPurchasedEventArgs : EventArgs
    {
        public string Category { get; }
        public string UpgradeId { get; }
        public int Level { get; }
        public UpgradeWithCategoryPurchasedEventArgs(string category, string upgradeId, int level)
        { Category = category; UpgradeId = upgradeId; Level = level; }
    }

    public class Analytics
    {
        // --- Debug flag
        public static bool DebugEnabled = false;

        // Events
        public static event EventHandler<LevelStartedEventArgs> LevelStarted;
        public static event EventHandler<LevelEndedEventArgs> LevelEnded;
        public static event EventHandler<WorldLevelStartedEventArgs> WorldLevelStarted;
        public static event EventHandler<WorldLevelEndedEventArgs> WorldLevelEnded;
        public static event EventHandler<UpgradePurchasedEventArgs> UpgradePurchasedEvent;
        public static event EventHandler<UpgradeWithCategoryPurchasedEventArgs> UpgradeWithCategoryPurchasedEvent;

        private static readonly object s_Sender = new object();

        // API
        public static void OnLevelStarted(int levelIndex)
        {
            if (DebugEnabled)
                Debug.Log($"[Analytics] Level started (index={levelIndex})");

            LevelStarted?.Invoke(s_Sender, new LevelStartedEventArgs(levelIndex));
        }

        public static void OnLevelEnded(int levelIndex, bool isLevelCompleted, float score)
        {
            if (DebugEnabled)
                Debug.Log($"[Analytics] Level ended (index={levelIndex}, completed={isLevelCompleted}, score={score})");

            LevelEnded?.Invoke(s_Sender, new LevelEndedEventArgs(levelIndex, isLevelCompleted, score));
        }

        public static void OnWorldLevelStarted(int worldIndex, int levelIndex)
        {
            if (DebugEnabled)
                Debug.Log($"[Analytics] World level started (world={worldIndex}, level={levelIndex})");

            WorldLevelStarted?.Invoke(s_Sender, new WorldLevelStartedEventArgs(worldIndex, levelIndex));
        }

        public static void OnWorldLevelEnded(int worldIndex, int levelIndex, bool isLevelCompleted, float score)
        {
            if (DebugEnabled)
                Debug.Log($"[Analytics] World level ended (world={worldIndex}, level={levelIndex}, completed={isLevelCompleted}, score={score})");

            WorldLevelEnded?.Invoke(s_Sender, new WorldLevelEndedEventArgs(worldIndex, levelIndex, isLevelCompleted, score));
        }

        public static void UpgradePurchased(string upgradeId, int level)
        {
            if (DebugEnabled)
                Debug.Log($"[Analytics] Upgrade purchased (id={upgradeId}, level={level})");

            UpgradePurchasedEvent?.Invoke(s_Sender, new UpgradePurchasedEventArgs(upgradeId, level));
        }

        public static void UpgradeWithCategoryPurchased(string category, string upgradeId, int level)
        {
            if (DebugEnabled)
                Debug.Log($"[Analytics] Upgrade purchased with category (category={category}, id={upgradeId}, level={level})");

            UpgradeWithCategoryPurchasedEvent?.Invoke(s_Sender, new UpgradeWithCategoryPurchasedEventArgs(category, upgradeId, level));
        }
    }
}
