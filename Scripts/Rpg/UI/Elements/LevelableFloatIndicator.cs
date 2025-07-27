using Kuantech.Core.UI;
using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Rpg.UI
{
    public class LevelableFloatIndicator : UIElement
    {
        
        [SerializeField] private Fillbar Fillbar;
        [SerializeField] private TMP_Text LevelText;
        [SerializeField] private string LevelTextPrefix = "";
        [SerializeField] private int LevelOffset = 0;

        public void UpdateValue(LevelVariable value)
        {
            if (value == null)
            {
                if(Fillbar != null) Fillbar.SetFill(0);
                return;
            }
            if (Fillbar != null)
            {
                float earnidThisLevel = value.GetEarnedThisLevel();
                float RequiredExp = value.GetRequiredFromCurrentToNextLevel();
                float percentage = value.GetCurrentProgressPercentage();
                float fillAmount = percentage;
                Fillbar.SetFill(fillAmount, earnidThisLevel, RequiredExp);
            }

            int level = value.CurrentLevel + LevelOffset;
            if (LevelText != null) LevelText.text = $"{LevelTextPrefix}{level.Stringfy()}";
        }
    }
}