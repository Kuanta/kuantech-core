using Kuantech.Core;
using Kuantech.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    public class MidcoreFailPanel : LevelFailPanel
    {
        [Header("Midcore Fail Panel")]
        [SerializeField] private Button ContinueButton;
        
        public override void Initialize()
        {
            base.Initialize();
            if (ContinueButton != null)
            {
                ContinueButton.onClick.AddListener(OnContinueButtonClicked);
            }
        }

        private void OnContinueButtonClicked()
        {
            GameManager.ChangeScene(MidcoreGameSceneManager.GetMenuSceneName());
        }
    }
}