using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Puzzle.UI
{
    public class BoosterButtonRemainingUseIndicator : MonoBehaviour
    {
        public TMP_Text RemainingUseText;

        public void SetRemainingUse(int use)
        {
            RemainingUseText.text = use.Stringfy();
        }
    }
}