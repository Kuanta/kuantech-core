using Kuantech.DragAndRace;
using Kuantech.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual
{
    public class MainMenu : UIMenu
    {
        [SerializeField] private Button StartLevelButton;

        protected virtual void Start()
        {
            StartLevelButton.onClick.AddListener(OnStartLevelButtonPressed);
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