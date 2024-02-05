using System;
using System.Collections.Generic;
using Kuantech.Puzzle.MatchThree.UI;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    [Serializable]
    public struct WinConditionEntry
    {
        public MatchThreeElementData RequiredElement;
        public int RequiredAmount;
    }

    public class MatchThreeLevel: PuzzleLevel
    {
        [Header("Board")]
        [SerializeField] protected MatchThreeBoard MatchThreeBoard;

        [Header("Level Properties")]
        [SerializeField] private int MaxMoveCount = 40;
        private int _currentMoveCount;
        public List<WinConditionEntry> WinCondition;
        private Dictionary<MatchThreeElementData, int> _collectedElements;
        private MatchThreeLevelUI _matchThreeLevelUI;
        public override void SetupLevel()
        {
            base.SetupLevel();
            _matchThreeLevelUI = ((MatchThreeLevelUI)LevelUI);
            MatchThreeBoard.Setup();
            MatchThreeBoard.OnMove += OnMove;
            PlayLevel(); //todo(matchemy): May not be good here
        }

        public override void PlayLevel()
        {
            base.PlayLevel();
            _currentMoveCount = MaxMoveCount;
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
            _matchThreeLevelUI.SetRemainingMoves(MaxMoveCount);
        }

        public override void ResetLevelState()
        {
            MatchThreeBoard.RestartBoard();
            _collectedElements.Clear();
            _currentMoveCount = MaxMoveCount;
            base.ResetLevelState();
        }

        /// <summary>
        /// Event handler for match3 board move
        /// </summary>
        public void OnMove()
        {
           ReduceRemainingMoveCount();
           _matchThreeLevelUI.SetRemainingMoves(_currentMoveCount);
        }

        /// <summary>
        /// Returns the maximum move count
        /// </summary>
        /// <returns></returns>
        public int GetMaxMoveCount()
        {
            return MaxMoveCount;
        }

        protected void ReduceRemainingMoveCount()
        {
            if (CheckForWinCondition()) return;
            _currentMoveCount--;
            if (_currentMoveCount <= 0)
            {
                //todo: Check if last move completed the level
                Debug.LogError("Fail level");
                FailLevel();
            }
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
        protected void AddToCollected(MatchThreeElementData data, int amount)
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
            if(WinCondition == null) return false;
            foreach(var entry in WinCondition)
            {
                int collectedAmount = GetCollectedElementCount(entry.RequiredElement);
                if(collectedAmount < entry.RequiredAmount) return false;
            }
            return true;
        }

        /// <summary>
        /// Checks whether an element is required for win condition
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool IsElementInWinCondition(MatchThreeElementData data)
        {
            if(WinCondition == null) return false;
            foreach(var entry in WinCondition)
            {
                if(entry.RequiredElement.IsSameType(data)) return true;
            }
            return false;
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