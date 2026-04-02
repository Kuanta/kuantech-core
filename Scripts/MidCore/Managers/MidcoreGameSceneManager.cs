using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// Manages the game scene affairs
    /// </summary>
    public class MidcoreGameSceneManager : SubManager
    {
        public string MenuSceneName = "MenuScene";
        public bool UseWorlds = false;
        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            var levelProgressionData = LevelProgressionStateManager.GetLevelProgressionData();
            //MidcoreSceneTransitionData transitionData = GameManager.GetLevelTransitionData() as MidcoreSceneTransitionData;
            int levelIndex = levelProgressionData.LevelIndex;
            int worldIndex = levelProgressionData.WorldIndex;

            LevelManager lm = GetContext<LevelManager>();
            if (lm == null)
            {
                Debug.LogWarning("Level manager is null");
                return;
            }

            if (UseWorlds)
            {
                lm.SetWorldLevel(worldIndex, levelIndex); //todo(levels): Implement world index
            }
            else
            {
                lm.SetLevel(levelIndex);       
            }
        }

        public static string GetMenuSceneName()
        {
            var ctx = GetContext<MidcoreGameSceneManager>();
            if (ctx == null) return "MenuScene";
            return ctx.MenuSceneName;
        }

        public static void GoToMenuScene()
        {
            Level currLevel = LevelManager.GetCurrentLevel();
            if (currLevel != null)
            {
                currLevel.QuitLevel();
            }
            string menuSceneName = GetMenuSceneName();
            GameManager.ChangeScene(menuSceneName);
        }
   
    }
}