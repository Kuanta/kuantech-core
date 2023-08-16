using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class RunnerManager : HCSubManager
    {
        public Runner Runner;
        public RunnerInputHandler RunnerInputHandler;
        
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            RunnerInputHandler.Runner = Runner;
            Runner.Initialize();
        }
        
        protected override void OnStateChange(object sender, StateChangeData stateChangeData)
        {
            if (stateChangeData.NewState == LevelState.Waiting)
            {
                Runner.OnMainMenu();
                Runner.transform.position = Vector3.zero;
                Runner.transform.rotation = Quaternion.identity;
            }
            if (stateChangeData.NewState == LevelState.Playing && stateChangeData.OldState == LevelState.Waiting)
            {
                RunnerInputHandler.enabled = true;
                Runner.OnPlay();
                Runner.transform.position = Vector3.zero;
                Runner.transform.rotation = Quaternion.identity;
            }
            else
            {
                RunnerInputHandler.enabled = false;
            }
            
            //An ugly but necessary fix
            if (stateChangeData.NewState == LevelState.Playing)
            {
                RunnerInputHandler.enabled = true;
            }
        }
    }
}