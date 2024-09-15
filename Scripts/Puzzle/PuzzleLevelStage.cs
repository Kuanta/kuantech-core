using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class PuzzleLevelStage
    {
        [Serializable]
        public struct WinConditionEntry
        {
            public string Key;
            public int TargetAmount;
            public bool ShowRemaining; //If set to true, shows the remainin amount in UI
        }
    
        public PuzzleLevelStage(List<PuzzleLevelStage.WinConditionEntry> conditions)
        {
            Targets = new Dictionary<string, WinConditionEntry>();
            CollectedAmounts = new Dictionary<string, int>();
            SetTargets(conditions);
        }
        
        public Dictionary<string, WinConditionEntry> Targets;
        public Dictionary<string, int> CollectedAmounts;
        
        public void SetTargets(List<WinConditionEntry> conditions)
        {
            foreach (var condition in conditions)
            {
                SetTarget(condition);
            }
        }
        
        /// <summary>
        /// Sets the target amoutn for the key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="targetAmount"></param>
        public void SetTarget(WinConditionEntry targetEntry)
        {
            if (Targets == null) Targets = new Dictionary<string, WinConditionEntry>();
            Targets[targetEntry.Key] = targetEntry;
        }

        public void SetTarget(string key, int value)
        {
            if (Targets == null) Targets = new Dictionary<string, WinConditionEntry>();
            if (Targets.ContainsKey(key))
            {
                WinConditionEntry  entry = Targets[key];
                entry.TargetAmount = value;
                Targets[key] = entry;
            }
            else
            {
                Targets[key] = new WinConditionEntry()
                {
                    Key = key,
                    ShowRemaining = true,
                    TargetAmount = value
                };
            }
        }
        
        /// <summary>
        /// Returns the target amount for the key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetTarget(string key)
        {
            if (Targets == null || !Targets.ContainsKey(key)) return 0;
            return Targets[key].TargetAmount;
        }

        public void SetCollectedAmount(string key, int amount)
        {
            if (CollectedAmounts == null)
            {
                CollectedAmounts = new Dictionary<string, int>();
            }

            CollectedAmounts[key] = amount;
        }
        
        public void AddCollectedAmount(string key, int amount)
        {
            int existingAmount = GetCollectedAmount(key);
            SetCollectedAmount(key, existingAmount+amount);
        }

        public int GetCollectedAmount(string key)
        {
            if (CollectedAmounts == null || !CollectedAmounts.ContainsKey(key)) return 0;
            return CollectedAmounts[key];
        }

        public int GetRemainingAmount(string key)
        {
            int target = GetTarget(key);
            int collected = GetCollectedAmount(key);
            return Mathf.Max(0, target - collected);
        }
        
        /// <summary>
        /// Checks if given condition is satisfied
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsConditionSatisfied(string key)
        {
            int targetAmount = GetTarget(key);
            int collectedAmount = GetCollectedAmount(key);
            return collectedAmount >= targetAmount;
        }
        
        /// <summary>
        /// Checks if all win conditions are satisfied
        /// </summary>
        /// <returns></returns>
        public bool IsStageCompleted()
        {
            foreach (var pair in Targets)
            {
                if (!IsConditionSatisfied(pair.Key)) return false;
            }

            return true;
        }
        public void Reset()
        {
            if (CollectedAmounts == null) return;
            CollectedAmounts.Clear();
        }

        #region State

        public StageState GetCurrentState()
        {
            StageState state = new StageState();
            state.CollectedAmounts = new List<int>();
            state.TargetKeys = new List<string>();
            foreach (var targetPair in Targets)
            {
                state.TargetKeys.Add(targetPair.Key);
                state.CollectedAmounts.Add(GetCollectedAmount(targetPair.Key));
            }

            return state;
        }

        public void LoadState(StageState state)
        {
            CollectedAmounts = new Dictionary<string, int>();
            for (int i = 0; i < state.TargetKeys.Count; ++i)
            {
                string key = state.TargetKeys[i];
                int collectedAmount = state.CollectedAmounts[i];
                SetCollectedAmount(key, collectedAmount);
            }
        }
        #endregion
    }
}