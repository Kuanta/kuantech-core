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
                Runner.transform.position = Vector3.zero;
                Runner.transform.rotation = Quaternion.identity;
            }
            if (stateChangeData.NewState == LevelState.Playing)
            {
                RunnerInputHandler.gameObject.SetActive(true);
                Runner.OnPlay();
                Runner.transform.position = Vector3.zero;
                Runner.transform.rotation = Quaternion.identity;
            }
            else
            {
                RunnerInputHandler.gameObject.SetActive(false);
            }
        }
    }
}