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
        [SerializeField] private Image MaskingImage;
        [SerializeField] private TMP_Text ScoreText;
        [NonSerialized] public bool ShowRemaining;

        [Header("Completed Elements")] 
        public GameObject CompletedElements;
        
        public void SetIcon(ColoredSpriteAsset iconSprite)
        {
            if (iconSprite == null) return;
            Icon.sprite = iconSprite.Sprite;
            Icon.color = iconSprite.GetColor();
            if (MaskingImage != null)
            {
                MaskingImage.sprite = iconSprite.MaskSprite;
            }
        }
        
        public void SetScore(int score, int remainingAmount)
        {
            bool completed = remainingAmount <= 0;
            ScoreText.gameObject.SetActive(!completed);
            CompletedElements.gameObject.SetActive(completed);
            ScoreText.text = ShowRemaining ? remainingAmount.Stringfy() : score.Stringfy();
        }
    }
}