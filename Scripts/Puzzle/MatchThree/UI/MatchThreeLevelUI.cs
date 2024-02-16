using System.Collections.Generic;
using Kuantech.Puzzle.UI;
using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree.UI
{
    public class MatchThreeLevelUI : PuzzleLevelUI
    {   
        [Header("Remaining Moves")]
        [SerializeField] private TMP_Text RemainingMovesText;

        [Header("Required Tiles")]
        [SerializeField] private MatchThreeRequiredTileIndicator TileIndicatorPrefab;
        [SerializeField] protected GameObject RequiredTilesPanel;
        [SerializeField] protected RectTransform RequiredTileIndicatorsContentParent;
        private Dictionary<MatchThreeElementData, MatchThreeRequiredTileIndicator> _elementToIndicator;

        private MatchThreeLevel _currentMatchThreeLevel;

        public override void OnLevelSetup(PuzzleLevel level)
        {
            base.OnLevelSetup(level);
            _currentMatchThreeLevel = (MatchThreeLevel)level;
            if (RequiredTileIndicatorsContentParent != null)
            {
                RequiredTileIndicatorsContentParent.DestroyAllChildren();

                _elementToIndicator = new Dictionary<MatchThreeElementData, MatchThreeRequiredTileIndicator>();
                if(_currentMatchThreeLevel.WinCondition.IsNullOrEmpty())
                {
                    RequiredTilesPanel.SetActive(false);
                }
                else{
                    RequiredTilesPanel.SetActive(true);
                    foreach (var winConditionEntry in _currentMatchThreeLevel.WinCondition)
                    {
                        MatchThreeRequiredTileIndicator indicator = Instantiate(TileIndicatorPrefab.gameObject)
                        .GetComponent<MatchThreeRequiredTileIndicator>();
                        _elementToIndicator[winConditionEntry.RequiredElement] = indicator;
                        indicator.SetElement(winConditionEntry.RequiredElement);
                        indicator.SetRemainingAmount(winConditionEntry.RequiredAmount);
                        indicator.transform.SetParent(RequiredTileIndicatorsContentParent, false); //The parent should have a layout component attached
                        indicator.transform.localScale = Vector3.one;
                    }
                }
            }
            SetRemainingMoves(_currentMatchThreeLevel.GetMaxMoveCount());
        }

        /// <summary>
        /// Updates the remaining amount for given data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="remainingAmount"></param>
        public void SetRemainingAmountForTileCondition(MatchThreeElementData data, int collectedAmount)
        {
            if(!_elementToIndicator.ContainsKey(data) || _elementToIndicator[data] == null) return;
            int remainingAmount = _currentMatchThreeLevel.GetRequiredCount(data) - collectedAmount;
            _elementToIndicator[data].SetRemainingAmount(Mathf.Max(0,remainingAmount));
        }

        /// <summary>
        /// Updates the remaining move text
        /// </summary>
        /// <param name="remainingMoves"></param>
        public void SetRemainingMoves(int remainingMoves)
        {
            if(RemainingMovesText != null) RemainingMovesText.text = remainingMoves.Stringfy();
        }
    }    
}