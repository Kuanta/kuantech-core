using System;
using System.Collections.Generic;
using Kuantech.Utils;
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
            public object UserData;
        }
    
        public PuzzleLevelStage(WinConditionTracker.LevelStageEntry stageEntry)
        {
            Targets = new Dictionary<string, WinConditionEntry>();
            CollectedAmounts = new Dictionary<string, int>();
            SetTargets(stageEntry.Conditions);
            SetFlatTarget(stageEntry.FlatScoreTarget);
        }
        
        public int FlatScoreTarget;
        public int CurrentFlatScore;
        public Dictionary<string, WinConditionEntry> Targets;
        public Dictionary<string, int> CollectedAmounts;

        #region Targets
        public void SetFlatTarget(int flatTarget)
        {
            FlatScoreTarget = flatTarget;
        }
        
        public void SetTargets(List<WinConditionEntry> conditions)
        {
            if (conditions.IsNullOrEmpty()) return;
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
            if (key == null) return FlatScoreTarget;
            if (Targets == null || !Targets.ContainsKey(key)) return 0;
            return Targets[key].TargetAmount;
        }
        #endregion

        #region Earnings
        public void SetCollectedAmount(string key, int amount)
        {
            if (CollectedAmounts == null)
            {
                CollectedAmounts = new Dictionary<string, int>();
            }

            CollectedAmounts[key] = amount;
        }
        
        public void AddScore(string key, int amount)
        {
            if (key == null)
            {
                AddFlatScore(amount);
                return;
            }
            int existingAmount = GetCollectedAmount(key);
            SetCollectedAmount(key, existingAmount+amount);
        }
        
        public int GetFlatScore()
        {
            return CurrentFlatScore;
        }
        public int GetCollectedAmount(string key)
        {
            if (key == null) return CurrentFlatScore;
            if (CollectedAmounts == null || !CollectedAmounts.ContainsKey(key)) return 0;
            return CollectedAmounts[key];
        }

        public int GetRemainingAmount(string key)
        {
            if (key == null)
            {
                return FlatScoreTarget - CurrentFlatScore;
            }
            int target = GetTarget(key);
            int collected = GetCollectedAmount(key);
            return Mathf.Max(0, target - collected);
        }

        public void AddFlatScore(int score)
        {
            CurrentFlatScore += score;
        }
        #endregion

       
        
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
            
            //All conditions are satisfied
            return CurrentFlatScore >= FlatScoreTarget;
        }
        public void Reset()
        {
            if (CollectedAmounts == null) return;
            CurrentFlatScore = 0;
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
            state.CurrentFlatScore = CurrentFlatScore;
            return state;
        }

        public void LoadState(StageState state)
        {
            CollectedAmounts = new Dictionary<string, int>();
            CurrentFlatScore = state.CurrentFlatScore;
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