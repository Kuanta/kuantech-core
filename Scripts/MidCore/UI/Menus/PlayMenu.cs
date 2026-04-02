using Kuantech.Core;
using Kuantech.Core.HyperCasual.UI;
using Kuantech.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    public class PlayMenu : UIMenu
    {
        [Header("Components")] [SerializeField]
        private Button PlayButton;

        [Header("World Theme")] [SerializeField]
        private LevelIndicator LevelIndicator;
        
        //Runtime
        private int _worldToPlay = -1;
        private int _levelToPlay = -1;

        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            
            PlayButton.onClick.AddListener(OnPlayButtonClicked);

            LevelProgressionStateManager lpm = LevelProgressionStateManager.GetContext<LevelProgressionStateManager>();
            if (lpm == null) return;
            lpm.CurrentLevelChanged -= OnCurrentLevelChanged;
            lpm.CurrentLevelChanged += OnCurrentLevelChanged;
        }

        public override void Show()
        {
            base.Show();
            SetCurrentWorldTheme();
        }
        
        protected virtual void SetCurrentWorldTheme()
        {
            LevelIndexData currentLevelData
                = LevelProgressionStateManager.GetLevelProgressionData();

            if (LevelIndicator != null)
            {
                LevelIndicator.SetLevel(currentLevelData);
            }
        }

        #region Handlers

        private void OnPlayButtonClicked()
        {
            MidcoreSceneTransitionData transitionData = new MidcoreSceneTransitionData()
            {
                LevelIndex = _levelToPlay,
                WorldIndex = _worldToPlay,
            };
            
            GameManager.ChangeScene(
                MidcoreMenuSceneManager.GetGameSceneName(),
                transitionData
            );
        }

        private void OnCurrentLevelChanged(LevelIndexData currentLevelData)
        {
            SetCurrentWorldTheme();
        }
        #endregion
    }
}