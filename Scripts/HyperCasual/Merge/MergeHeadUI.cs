using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Merge
{
    public class MergeHeadUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text LevelText;
        [SerializeField] private Image Icon;
        [SerializeField] private Image BackgroundFrame;
        [SerializeField] private Canvas Canvas;
        public void SetText(string text)
        {
            if (LevelText != null) LevelText.text = text;
        }

        public void SetIcon(Sprite icon)
        { 
            if(Icon != null) Icon.sprite = icon;
        }

        public void SetBackgroundColor(Color color)
        {
            if (BackgroundFrame != null) BackgroundFrame.color = color;
        }

    }
}