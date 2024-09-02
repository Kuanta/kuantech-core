using System;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Puzzle.UI
{
    public class WinConditionIndicatorElement : MonoBehaviour
    {
        [SerializeField] private Image Icon;
        [SerializeField] private TMP_Text ScoreText;
        [NonSerialized] public bool ShowRemaining;
        public void SetIcon(ColoredSpriteAsset iconSprite)
        {
            Icon.sprite = iconSprite.Sprite;
            Icon.color = iconSprite.GetColor();
        }
        
        public void SetScore(int score, int remainingAmount)
        {
            ScoreText.text = ShowRemaining ? remainingAmount.Stringfy() : score.Stringfy();
        }
    }
}