using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Puzzle
{
    /// <summary>
    /// Win condition tracker, tracks the win condition for the level
    /// </summary>
    public class WinConditionTracker
    {
        [Serializable]
        public struct LevelStageEntry
        {
            public List<PuzzleLevelStage.WinConditionEntry> Conditions;
        }

        public bool AutoCompleteStage = true; //If set true, stages will be completed automatically.
        public int StageCount;
        [NonSerialized] public List<PuzzleLevelStage> Stages = null;
        private int _currentStageIndex = 0;
        
        //Events
        public UnityAction<int> OnStageCompleted;
        public UnityAction<int> OnNewStage;
        public UnityAction OnAllStagesCompleted;
        
        public void SetStages(List<LevelStageEntry> stages)
        {
            Stages = new List<PuzzleLevelStage>();
            foreach (var stage in stages)
            {
                AddStage(stage);
            }
        }

        public void AddStage(LevelStageEntry stageEntry)
        {
            Stages.Add(new PuzzleLevelStage(stageEntry.Conditions));
        }
        
        public int GetStageCount()
        {
            if(Stages == null) return StageCount;
            return Stages.Count;
        }
        
        public int GetCurrentStageIndex()
        {
            return _currentStageIndex;
        }
        
        public void AdvanceStage()
        {
            int stageCount = GetTotalStageCount();
            if (_currentStageIndex == stageCount - 1)
            {
                //All stages are completed
                OnAllStagesCompleted?.Invoke();
                return;
            }
            OnStageCompleted?.Invoke(_currentStageIndex);
            GoToStage(_currentStageIndex + 1);
        }

        public bool IsCurrentStageCompleted()
        {
            return GetCurrentStage().IsStageCompleted();
        }
        
        /// <summary>
        /// Returns the stage at given index
        /// </summary>
        /// <param name="stageIndex"></param>
        /// <returns></returns>
        public PuzzleLevelStage GetStage(int stageIndex)
        {
            if (stageIndex >= GetTotalStageCount())
            {
                return null;
            }

            return Stages[stageIndex];
        }
        
        /// <summary>
        /// Sets the stage
        /// </summary>
        /// <param name="stageIndex"></param>
        /// <returns></returns>
        public bool GoToStage(int stageIndex)
        {
            if (stageIndex >= GetTotalStageCount()) return false;
            _currentStageIndex = stageIndex;
            OnNewStage?.Invoke(stageIndex);
            return true;
        }

        private int GetTotalStageCount()
        {
            return Stages?.Count ?? StageCount;
        }
        
        public PuzzleLevelStage GetCurrentStage()
        {
            return Stages[Mathf.Min(_currentStageIndex, Stages.Count-1)];
        }
        
        /// <summary>
        /// Adds score to given key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="amount"></param>
        public void AddScore(string key, int amount)
        {
            PuzzleLevelStage currentStage = GetCurrentStage();
            currentStage.AddScore(key, amount);
            if (currentStage.IsStageCompleted() && AutoCompleteStage)
            {
                AdvanceStage();
            }
        }
        
        /// <summary>
        /// Returns the currently collected amount of recourse for current
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetCollectedAmount(string key)
        {
            return GetCurrentStage().GetCollectedAmount(key);
        }
        
        /// <summary>
        /// Returns the target for current
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetTargetAmount(string key)
        {
            return GetCurrentStage().GetTarget(key);
        }

        public int GetRemainingAmount(string key)
        {
            return GetTargetAmount(key) - GetCollectedAmount(key);
        }
        /// <summary>
        /// Checks if all win conditions are satisfied
        /// </summary>
        /// <returns></returns>
        public bool CheckWinCondition()
        {
            //Don't need to loop, but looping anyway
            foreach (var stage in Stages)
            {
                if (stage.IsStageCompleted()) return false;
            }

            return true;
        }
        
        public void Reset(bool resetStageIndex)
        {
            if (Stages != null)
            {
                foreach (var stage in Stages)
                {
                    stage.Reset();
                }
            }
            if(resetStageIndex) _currentStageIndex = 0;
        }

        #region State
        /// <summary>
        /// Gets the states
        /// </summary>
        /// <returns></returns>
        public WinConditionTrackerState GetCurrentState()
        {
            WinConditionTrackerState state = new WinConditionTrackerState();
            state.CurrentStageIndex = _currentStageIndex;
            state.CurrentStageState = GetCurrentStage().GetCurrentState();
            return state;
        }
        
        /// <summary>
        /// Loads the state
        /// </summary>
        /// <param name="state"></param>
        public void LoadState(WinConditionTrackerState state)
        {
            GoToStage(state.CurrentStageIndex);
            GetCurrentStage().LoadState(state.CurrentStageState);
        }
        #endregion

        #region Target Scores
        public void SetTargetScoreForStage(int stageIndex, string key, int targetAmount)
        {
            PuzzleLevelStage stage = GetStage(stageIndex);
            if (stage == null) return;
            stage.SetTarget(key, targetAmount);
        }
        #endregion
    }
}