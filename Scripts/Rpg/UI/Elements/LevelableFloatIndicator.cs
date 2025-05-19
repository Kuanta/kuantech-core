using Kuantech.Core.UI;
using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Rpg.UI
{
    public class LevelableFloatIndicator : KtUIElement
    {
        
        [SerializeField] private Fillbar Fillbar;
        [SerializeField] private TMP_Text LevelText;

        public void UpdateValue(LevelVariable value)
        {
            if (value == null)
            {
                if(Fillbar != null) Fillbar.SetFill(0);
            }
            if (Fillbar != null)
            {
                float earnidThisLevel = value.GetEarnedThisLevel();
                float RequiredExp = value.GetRequiredFromCurrentToNextLevel();
                float percentage = value.GetCurrentProgressPercentage();
                float fillAmount = percentage;
                Debug.Log("Earned this level: " + earnidThisLevel+"RequiredExp: " + RequiredExp);
                Fillbar.SetFill(fillAmount, earnidThisLevel, RequiredExp);
            }
            if(LevelText != null) LevelText.text = value.CurrentLevel.Stringfy();
        }
    }
}