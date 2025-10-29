using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Midcore.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    //todo: Remove this class completely
    public class MidcoreMenuSceneManager : SubManager
    {
        // [SaveableField] public int CurrentWorldIndex;
        // [SaveableField] public int CurrentLevelIndex;
        
        //todo: Remove this from here
        [Header("UI elements")] [SerializeField]
        private FreshUnlocksPanel FreshUnlocksPanel;
        
        //todo: remove this from here
        public string GameSceneName = "GameScene";

        [Header("Theme Music")] [SerializeField]
        private Music MenuMusic;
        
        public override void OnSubmanagersInitialized()
        {
            if (FreshUnlocksPanel == null) return;
            HashSet<CollectableAsset> freshCollectables = ProgressionManager.GetFreshCollectables();
            if (freshCollectables.IsNullOrEmpty())
            {
                FreshUnlocksPanel.gameObject.SetActive(false);
            }

            {
                FreshUnlocksPanel.Show();
                FreshUnlocksPanel.ShowFreshUnlocks(freshCollectables);
            }
            
            //Clear fresh collectibles after showing them
            ProgressionManager.ClearFreshCollectables();
            
            //Play menu music
            EffectsLibrary.GetAudioLibrary().PlayMusic(MenuMusic);
        }
        
        #region Level
        // public static int GetCurrentWorldIndex()
        // {
        //     var ctx = GetContext<MidcoreMenuSceneManager>();
        //     if (ctx == null) return 0;
        //     return ctx.CurrentWorldIndex;
        // }
        //
        // public static int GetCurrentLevelIndex()
        // {
        //     var ctx = GetContext<MidcoreMenuSceneManager>();
        //     if (ctx == null) return 0;
        //     return ctx.CurrentLevelIndex;
        // }
        //
        // public static void SetWorldIndex(int worldIndex)
        // {
        //     var ctx = GetContext<MidcoreMenuSceneManager>();
        //     if (ctx == null) return;
        //     ctx.CurrentWorldIndex = worldIndex;
        // }
        //
        // public static void SetLevelIndex(int levelIndex)
        // {
        //     var ctx = GetContext<MidcoreMenuSceneManager>();
        //     if (ctx == null) return;
        //     ctx.CurrentLevelIndex = levelIndex;
        // }
        
        public static string GetGameSceneName()
        {
            var ctx = GetContext<MidcoreMenuSceneManager>();
            if (ctx == null) return "GameScene";
            return ctx.GameSceneName;
        }
        
        #endregion
    }
}