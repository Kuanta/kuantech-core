using System.Collections.Generic;
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
      
            //Go to main menu
            MidcoreGameSceneManager.GoToMenuScene();
        }
    }
}