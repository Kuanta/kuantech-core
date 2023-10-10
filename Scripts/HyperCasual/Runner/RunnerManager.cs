using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    [RequireComponent(typeof(LevelManager))]
    public class RunnerManager : SubManager
    {
        public Runner Runner;
        public RunnerInputHandler RunnerInputHandler;
        
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            RunnerInputHandler.Runner = Runner;
            Runner.Initialize();
        }

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            LevelManager levelMan = GameManager.Instance.GetSubManagerByType<LevelManager>() as LevelManager;
            levelMan.StateChangeEvent += OnStateChange;
        }

        private void OnStateChange(object sender, StateChangeData stateChangeData)
        {
            if (stateChangeData.NewState == LevelState.Waiting)
            {
                Runner.OnMainMenu();
                Runner.transform.position = Vector3.zero;
                Runner.transform.rotation = Quaternion.identity;
            }
            if (stateChangeData.NewState == LevelState.Playing && stateChangeData.OldState == LevelState.Waiting)
            {
                Runner.OnPlay();
                Runner.transform.position = Vector3.zero;
                Runner.transform.rotation = Quaternion.identity;
            }
            
            //An ugly but necessary fix
            if (stateChangeData.NewState == LevelState.Playing)
            {
                RunnerInputHandler.enabled = true;
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            LevelManager levelMan = GameManager.Instance.GetSubManagerByType<LevelManager>() as LevelManager;
            levelMan.StateChangeEvent -= OnStateChange;
        }
    }
}