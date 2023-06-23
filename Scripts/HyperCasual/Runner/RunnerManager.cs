using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class RunnerManager : SubManager
    {
        public Runner Runner;
        public RunnerInputHandler RunnerInputHandler;
        
        public override void Initialize(HCGameManager hcGameManager)
        {
            base.Initialize(hcGameManager);
            RunnerInputHandler.Runner = Runner;
        }
        
        protected override void OnStateChange(object sender, StateChangeData stateChangeData)
        {
            if (stateChangeData.NewState == LevelState.Playing)
            {
                RunnerInputHandler.gameObject.SetActive(true);
                RunnerLevel currentLevel = HcGameManager.CurrentLevel as RunnerLevel;
                currentLevel.SetRunner(Runner);
            }
            else
            {
                RunnerInputHandler.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (HcGameManager.CurrentLevel != null && (HcGameManager.CurrentLevel.CurrentState == LevelState.Playing)
                    || (HcGameManager.CurrentLevel.CurrentState == LevelState.Failed))
                {
                    HcGameManager.RestartLevel();
                }
            }
        }
    }
}