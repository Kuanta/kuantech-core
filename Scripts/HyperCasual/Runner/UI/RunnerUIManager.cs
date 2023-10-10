using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual.Runner.UI
{
    public class RunnerUIManager : SubManager
    {
        //Panels
        public RunnerMainMenu MainMenu;
        public RunnerIngameMenu IngameMenu;

        private Vector2 _scaledScreenSize;
        public override void OnSubmanagersInitialized()
        {
            LevelManager levelMan = GameManager.Instance.GetSubManagerByType<LevelManager>() as LevelManager;
            if(levelMan == null) return;
            levelMan.StateChangeEvent += OnStateChange;
            MainMenu.Initialize();
            IngameMenu.Initialize();
        }

        private void OnStateChange(object sender, StateChangeData change)
        {
            IngameMenu.OnStateChange(change.NewState);
            MainMenu.OnStateChange(change.NewState);
            if (change.NewState == LevelState.Waiting)
            {
                IngameMenu.Close();
                MainMenu.Show();
            }
            else if(change.NewState == LevelState.Playing)
            {
                IngameMenu.Show();
                MainMenu.Close();
            }
        }

        public float GetScreenWidth()
        {
            return _scaledScreenSize.x;
        }

        public float GetScreenHeight()
        {
            return _scaledScreenSize.y;
        }

        private void OnEnable()
        {
            _scaledScreenSize = GetScaledScreenSize();
        }

        public Vector2 GetScaledScreenSize()
        {
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            Vector2 refResolution = scaler.referenceResolution;
            float scaleFactorWidth = Screen.width / refResolution.x;
            float scaleFactorHeight = Screen.height / refResolution.y;
            if (scaler.matchWidthOrHeight == 0)
            {
                return new Vector2(Screen.width / scaleFactorWidth, Screen.height / scaleFactorWidth);

            }
            else
            {
                return new Vector2(Screen.width / scaleFactorHeight, Screen.height / scaleFactorHeight);

            }
        }
    }
}