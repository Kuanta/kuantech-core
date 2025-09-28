using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Rpg
{
    [Serializable]
    public struct LevelVariableData
    {
        public float BaseRequirement;
        public float GrowthFactor;
    }
    
    [Serializable]
    public class LevelVariable : ISaveable
    {
        [SerializeField] private float baseRequirement = 100f;
        [SerializeField] private float growthFactor = 1.5f;
        public float TotalValue = 0.0f;
        public int CurrentLevel => CalculateLevel(TotalValue);
        public float ValueIntoCurrentLevel => TotalValue - GetTotalRequiredForLevel(CurrentLevel);
        public float ValueToNextLevel => GetTotalRequiredForLevel(CurrentLevel + 1) - TotalValue;
        public float CurrentLevelRequirement => GetTotalRequiredForLevel(CurrentLevel + 1) - GetTotalRequiredForLevel(CurrentLevel);

        public EventHandler<(int, int)> OnLevelUp;

        public LevelVariable(LevelVariableData lvd)
        {
            baseRequirement = lvd.BaseRequirement;
            growthFactor = lvd.GrowthFactor;
        }
        
        public void SetBaseRequirement(float newBaseRequirement)
        {
            baseRequirement = Mathf.Max(0f, newBaseRequirement);
        }
        
        public void SetGrowthFactor(float newGrowthFactor)
        {
            growthFactor = Mathf.Max(0f, newGrowthFactor);
        }
        
        public void AddValue(float amount)
        {
            int oldLevel = CurrentLevel;
            TotalValue = Mathf.Max(0f, TotalValue + amount);
            int newLevel = CurrentLevel;
            if (newLevel > oldLevel)
            {
                OnLevelUp?.Invoke(this, (oldLevel, newLevel));
            }
        }
        
        public void SetValue(float newTotal)
        {
            TotalValue = Mathf.Max(0f, newTotal);
        }
        
        /// <summary>
        /// Sets the level
        /// </summary>
        /// <param name="newLevel"></param>
        public void SetLevel(int newLevel)
        {
            TotalValue = GetTotalRequiredForLevel(newLevel);
        }
        
        /// <summary>
        /// Levels up
        /// </summary>
        public void LevelUp()
        {
            TotalValue = GetTotalRequiredForLevel(CurrentLevel + 1);
        }
        public void Reset()
        {
           TotalValue = 0f;
        }

        private int CalculateLevel(float total)
        {
            int level = 0;
            
            while (total >= GetTotalRequiredForLevel(level + 1))
            {
                level++;
            }
            return level;
        }

        public float GetRequiredFromCurrentToNextLevel()
        {
            return GetTotalRequiredForLevel(CurrentLevel + 1) - GetTotalRequiredForLevel(CurrentLevel);
        }

        public float GetEarnedThisLevel()
        {
            return TotalValue - GetTotalRequiredForLevel(CurrentLevel);
        }
        
        /// <summary>
        /// How much is filled to reach next level
        /// </summary>
        /// <returns></returns>
        public float GetCurrentProgressPercentage()
        {
            float expForLevelUp = GetRequiredFromCurrentToNextLevel();
            float earned = GetEarnedThisLevel();
            return Mathf.Clamp01(earned / expForLevelUp );
        }
        
        // Tek bir level'ın ne kadar gerektirdiği
        public float GetRequiredForSingleLevel(int level)
        {
            if (level <= 0) return 0;
            return Mathf.Max(baseRequirement, 1) * Mathf.Pow(level, growthFactor);
        }
        
        
        // Seviye 1'den belirtilen levele kadar toplamda gereken
        public float GetTotalRequiredForLevel(int level)
        {
            float sum = 0f;
            if (level <= 0) return 0.0f;
            for (int i = 1; i <= level; i++)
            {
                sum += GetRequiredForSingleLevel(i);
            }
            return sum;
        }

        public byte[] Serialize()
        {
            return null;
        }

        public void Deserialize(byte[] data)
        {
            return;
        }
    }
}