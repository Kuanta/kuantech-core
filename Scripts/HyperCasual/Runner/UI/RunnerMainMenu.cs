using Kuantech.Core.UI;
using Kuantech.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual.Runner.UI
{
    public class RunnerMainMenu : UIMenu
    {
        [SerializeField] private Button StartLevelButton;

        protected virtual void Start()
        {
            if (StartLevelButton == null) return;
            StartLevelButton.onClick.AddListener(OnStartLevelButtonPressed);
        }

        public virtual void Initialize()
        {
            
        }
        private void OnStartLevelButtonPressed()
        {
            LevelManager.GetContext<LevelManager>().StartLevel();
        }

  
        public void OnStateChange(LevelState newState)
        {
            
        }
    }
}