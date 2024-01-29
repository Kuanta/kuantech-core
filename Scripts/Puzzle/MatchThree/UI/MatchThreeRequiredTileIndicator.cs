using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Puzzle.MatchThree.UI
{
    public class MatchThreeRequiredTileIndicator : MonoBehaviour {

        [SerializeField] private Image Icon;
        [SerializeField] private TMP_Text RemainingAmount;

        public void SetRemainingAmount(int remainingAmount)
        {
            RemainingAmount.text = remainingAmount.Stringfy();
        }
        
        public void SetElement(MatchThreeElementData data)
        {
            Icon.sprite = data.Icon;
        }        
    }
}