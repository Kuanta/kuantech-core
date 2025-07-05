using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.UI;
using Kuantech.HyperCasual.UI;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class MidcoreCompletePanel :CompletePanel
    {
        [SerializeField] private List<CurrencyIndicator> CurrencyIndicators;
        
        public override void OnCompleteLevelButton()
        {
            base.OnCompleteLevelButton();
            Level currentLevel = LevelManager.GetCurrentLevel();
            if (currentLevel != null)
            {
                currentLevel.ClearLevel();
            }
            //Go to main menu
            string menuSceneName = MidcoreGameSceneManager.GetMenuSceneName();
            GameManager.ChangeScene(menuSceneName);
        }
    }
}