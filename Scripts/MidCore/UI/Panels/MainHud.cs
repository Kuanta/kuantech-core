using System.Collections.Generic;
using Kuantech.Core.UI;
using Kuantech.Rpg;
using Kuantech.Rpg.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class MainHud : UIMenu
    {
        [Header("Components")] 
        public LevelableFloatIndicator PlayerLevelBar;
        [SerializeField] private List<MenuOpenButton> MenuButtons;
        public List<UIElement> ElementsToInitialize;
        
        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            if (PlayerLevelBar != null)
            {
                ProgressionManager pm = ProgressionManager.GetContext<ProgressionManager>();
                if (pm != null)
                {
                    pm.OnPlayerEarnedExperience += OnPlayerEarnedExperienceHandler;
                }
                UpdatePlayerLevelBar();
            }

            if (MenuButtons.IsNullOrEmpty()) return;
            foreach (var button in MenuButtons)
            {
                button.Initialize();
            }

            foreach (var element in ElementsToInitialize)
            {
                element.Initialize();
            }
        }

        public override void Show()
        {
            base.Show();
            UpdatePlayerLevelBar();
        }
        
        #region Components Update
        private void UpdatePlayerLevelBar()
        {
            if (PlayerLevelBar == null)
            {
                return;
            }

            PlayerLevelBar.UpdateValue(ProgressionManager.GetPlayerLevel());
        }
        #endregion
        
        #region Handlers
        private void OnPlayerEarnedExperienceHandler(LevelVariable playerLevel)
        {
            UpdatePlayerLevelBar();
        }
        #endregion
  
    }
}