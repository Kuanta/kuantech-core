using Kuantech.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual
{
    public class LevelFailedPanel : UIMenu
    {
        [SerializeField] private Button RestartLevelButton;

        private void Start()
        {
            RestartLevelButton.onClick.AddListener(OnRestartLevelButton);
        }

        private void OnRestartLevelButton()
        {
            ((HCGameManager)HCGameManager.Instance).RestartLevel();
        }
    }
}