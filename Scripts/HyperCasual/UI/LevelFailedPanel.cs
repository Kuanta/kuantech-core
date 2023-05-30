using Kuantech.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual
{
    public class LevelFailedPanel : UIMenu
    {
        [SerializeField] private Button RestartLevelButton;

        public void Initialize()
        {
            if(RestartLevelButton != null) RestartLevelButton.onClick.AddListener(OnRestartLevelButton);
        }

        private void OnRestartLevelButton()
        {
            ((HCGameManager)HCGameManager.Instance).RestartLevel();
        }
    }
}