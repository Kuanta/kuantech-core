using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// Manages the game scene affairs
    /// </summary>
    public class MidcoreGameSceneManager : SubManager
    {
        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            
            MidcoreSceneTransitionData transitionData = GameManager.GetLevelTransitionData() as MidcoreSceneTransitionData;
            int levelIndex = 0;
            int worldIndex = 0;
            if (transitionData != null)
            {
                worldIndex = transitionData.WorldIndex;
                levelIndex = transitionData.LevelIndex;
            }

            LevelManager lm = LevelManager.GetContext<LevelManager>();
            if (lm == null)
            {
                Debug.LogWarning("Level manager is null");
                return;
            }
            lm.SetLevel(levelIndex); //todo(levels): Implement world index
        }
    }
}