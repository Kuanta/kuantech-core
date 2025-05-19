using Kuantech.Core.UI;
using Kuantech.Rpg;
using Kuantech.Rpg.UI;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class MainHud : UIMenu
    {
        [Header("Componentes")] 
        public LevelableFloatIndicator PlayerLevelBar;

        public override void Initialize()
        {
            base.Initialize();
            if (PlayerLevelBar != null)
            {
                ProgressionManager pm = ProgressionManager.GetContext<ProgressionManager>();
                pm.OnPlayerEarnedExperience += OnPlayerEarnedExperienceHandler;
                UpdatePlayerLevelBar();
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