using Kuantech.Core;
using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class PlayButton : MonoBehaviour, IUIButtonAction
    {
        public void OnClick()
        {
            //Change scene
            MidcoreSceneTransitionData transitionData = new MidcoreSceneTransitionData()
            {
                LevelIndex = MidcoreMenuSceneManager.GetCurrentLevelIndex(),
                WorldIndex = MidcoreMenuSceneManager.GetCurrentWorldIndex(),
            };

            GameManager.ChangeScene(
                MidcoreMenuSceneManager.GetGameSceneName(),
                transitionData
            );
        }
    }
}