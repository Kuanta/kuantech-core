using System;
using System.Collections.Generic;
using Kuantech.Puzzle.MatchThree.UI;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    // [Serializable]
    // public struct WinConditionEntry
    // {
    //     public MatchThreeElementData RequiredElement;
    //     public int RequiredAmount;
    // }

    public class MatchThreeLevel: PuzzleLevel
    {
        [Header("Board")]
        [SerializeField] protected MatchThreeBoard MatchThreeBoard;

        [Header("Level Properties")]
        public int MaxMoveCount = 40;
        protected int CurrentMoveCount;
        //public List<PuzzleLevelStage.WinConditionEntry> WinCondition;
        private Dictionary<MatchThreeElementData, int> _elementToRequiredCount;
        private Dictionary<MatchThreeElementData, int> _collectedElements;
        private MatchThreeLevelUI _matchThreeLevelUI;

        public WinConditionTracker WinConditionTracker;
        
        public override void SetupLevel()
        {
            base.SetupLevel();
            _elementToRequiredCount = new Dictionary<MatchThreeElementData, int>();
            // foreach(var condition in WinCondition)
            // {
            //     _elementToRequiredCount[condition.RequiredElement] = condition.RequiredAmount;
            // }
            _matchThreeLevelUI = ((MatchThreeLevelUI)LevelUI);
            MatchThreeBoard.Setup();
            MatchThreeBoard.OnMove += OnMove;
        }

        protected override void PlayLevel()
        {
            base.PlayLevel();
            CurrentMoveCount = MaxMoveCount;
            if(_collectedElements != null)
            {
                _collectedElements.Clear();
            }else{
                _collectedElements = new Dictionary<MatchThreeElementData, int>();
            }
        }

        protected override void ResetUI()
        {
            base.ResetUI();
            _matchThreeLevelUI = ((MatchThreeLevelUI)LevelUI);
            if(_matchThreeLevelUI == null) return;
            _matchThreeLevelUI.SetRemainingMoves(MaxMoveCount);
            _matchThreeLevelUI.ResetWinConditionPanel();
        }

        public override void ResetLevelState()
        {
            MatchThreeBoard.RestartBoard();
            _collectedElements.Clear();
            CurrentMoveCount = MaxMoveCount;
            base.ResetLevelState();
        }

        /// <summary>
        /// Event handler for match3 board move
        /// </summary>
        public void OnMove()
        {
           ReduceRemainingMoveCount();
        }

        /// <summary>
        /// Returns the maximum move count
        /// </summary>
        /// <returns></returns>
        public int GetMaxMoveCount()
        {
            return MaxMoveCount;
        }

        public void ReduceRemainingMoveCount()
        {
            if (CheckForWinCondition()) return;
            CurrentMoveCount--;
            if (CurrentMoveCount <= 0)
            {
                //todo: Check if last move completed the level
                Debug.LogError("Fail level");
                FailLevel();
            }
            _matchThreeLevelUI.SetRemainingMoves(CurrentMoveCount);
        }

        /// <summary>
        /// Adds collected amounts
        /// </summary>
        /// <param name="collectionTuple"></param>
        public void OnElementsCollected((MatchThreeElementData, int) collectionTuple)
        {
            MatchThreeElementData data = collectionTuple.Item1;
            int amount = collectionTuple.Item2;
            AddToCollected(data, amount);
        }

        /// <summary>
        /// Adds the amount as collected for the given data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="amount"></param>
        protected virtual void AddToCollected(MatchThreeElementData data, int amount)
        {
            if (!_collectedElements.ContainsKey(data))
            {
                _collectedElements[data] = 0;
            }
            _collectedElements[data] += amount;
            _matchThreeLevelUI.SetRemainingAmountForTileCondition(data, _collectedElements[data]);
        }

        /// <summary>
        /// Chekcs whether the win condition is met
        /// </summary>
        /// <returns></returns>
        public virtual bool IsWinConditionMet()
        {
            // if(WinCondition == null) return false;
            // foreach(var entry in WinCondition)
            // {
            //     if(!IsWinConditionMetForElement(entry.RequiredElement)) return false;
            // }
            // return true;
            return WinConditionTracker.CheckWinCondition();
        }

        public bool IsWinConditionMetForElement(MatchThreeElementData data)
        {
            int collectedAmount = GetCollectedElementCount(data);
            int RequiredAmount = GetRequiredCount(data);
            return RequiredAmount - collectedAmount <= 0f;
        }

        public int GetRequiredCount(MatchThreeElementData data)
        {
            if(_elementToRequiredCount == null || !_elementToRequiredCount.ContainsKey(data)) return 0;
            return _elementToRequiredCount[data];
        }

        /// <summary>
        /// Checks if level is completed. Completes the level if so.
        /// </summary>
        /// <returns>True if level is completed</returns>
        protected bool CheckForWinCondition()
        {
            if(!IsWinConditionMet()) return false;
            CompleteLevel();
            return true;
        }

        /// <summary>
        /// Returns the number of collected elements for the given data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected int GetCollectedElementCount(MatchThreeElementData data)
        {
            if(_collectedElements == null || !_collectedElements.ContainsKey(data)) return 0;
            return _collectedElements[data];
        }
    }
}