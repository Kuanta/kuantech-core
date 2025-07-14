using Kuantech.Core;

namespace Kuantech.Midcore
{
    public class MidcoreMenuSceneManager : SubManager
    {
        public int CurrentWorldIndex;
        public int CurrentLevelIndex;
        
        public string GameSceneName = "GameScene";
        
        #region Level
        public static int GetCurrentWorldIndex()
        {
            var ctx = GetContext<MidcoreMenuSceneManager>();
            if (ctx == null) return 0;
            return ctx.CurrentWorldIndex;
        }

        public static int GetCurrentLevelIndex()
        {
            var ctx = GetContext<MidcoreMenuSceneManager>();
            if (ctx == null) return 0;
            return ctx.CurrentLevelIndex;
        }

        public static void SetWorldIndex(int worldIndex)
        {
            var ctx = GetContext<MidcoreMenuSceneManager>();
            if (ctx == null) return;
            ctx.CurrentWorldIndex = worldIndex;
        }

        public static void SetLevelIndex(int levelIndex)
        {
            var ctx = GetContext<MidcoreMenuSceneManager>();
            if (ctx == null) return;
            ctx.CurrentLevelIndex = levelIndex;
        }
        
        public static string GetGameSceneName()
        {
            var ctx = GetContext<MidcoreMenuSceneManager>();
            if (ctx == null) return "GameScene";
            return ctx.GameSceneName;
        }
        #endregion
    }
}