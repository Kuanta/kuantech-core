using System;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class RunnerManager : SubManager
    {
        public Runner RunnerPrefab;
        [NonSerialized] public Runner Runner;
        
        [Header("Input Handler")]
        [SerializeField] private RunnerInputHandler RunnerInputHandler;

        [Header("Cameras")]
        [SerializeField] private CinemachineVirtualCamera MainMenuCamera;
        [SerializeField] private CinemachineVirtualCamera FollowCamera;

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            Runner = Instantiate(RunnerPrefab.gameObject).GetComponent<Runner>();
            Runner.Initialize();
            RunnerInputHandler.Runner = Runner;
            MainMenuCamera.Follow = Runner.GetFollowTarget();
            MainMenuCamera.LookAt = Runner.GetFollowTarget();
            FollowCamera.Follow = Runner.GetFollowTarget();
            FollowCamera.LookAt = Runner.GetFollowTarget();

            MainMenuCamera.enabled = true;
            FollowCamera.enabled = false;
        }

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            LevelManager levelMan = GameManager.Instance.GetSubManagerByType<LevelManager>() as LevelManager;
            levelMan.StateChangeEvent += OnStateChange;
        }

        private void OnStateChange(object sender, LevelStateChangeData stateChangeData)
        {
            if (stateChangeData.NewState == LevelState.Waiting)
            {
                Runner.OnMainMenu();
                Runner.transform.position = Vector3.zero;
                Runner.transform.rotation = Quaternion.identity;
                MainMenuCamera.enabled = true;
                FollowCamera.enabled = false;
            }
            if (stateChangeData.NewState == LevelState.Playing && stateChangeData.OldState == LevelState.Waiting)
            {
                Runner.OnPlay();
                Runner.transform.position = Vector3.zero;
                Runner.transform.rotation = Quaternion.identity;
                MainMenuCamera.enabled = false;
                FollowCamera.enabled = true;
            }
        }

        public static Runner GetCurrentRunner()
        {
            var context = GetContext<RunnerManager>();
            return context.Runner;
        }
        public override void Cleanup()
        {
            base.Cleanup();
            LevelManager levelMan = GameManager.Instance.GetSubManagerByType<LevelManager>() as LevelManager;
            levelMan.StateChangeEvent -= OnStateChange;
            Runner.gameObject.SetActive(false);
        }
    }
}