using Kuantech.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual.UI
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
            LevelManager.GetContext<LevelManager>().RestartLevel();
        }
    }
}