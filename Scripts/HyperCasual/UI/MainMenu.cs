using Kuantech.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual
{
    public class MainMenu : UIMenu
    {
        [SerializeField] private Button StartLevelButton;

        private void Start()
        {
            StartLevelButton.onClick.AddListener(OnStartLevelButtonPressed);
        }
    
        public override void Show()
        {
            base.Show();
        }
        
        public override void Close()
        {
        
            base.Close();
        }
        
        private void OnStartLevelButtonPressed()
        {
            ((HCGameManager)GameManager.Instance).PlayLevel();
        }
        
        public void OnStateChange(LevelState newState)
        {
            
        }
    }
}