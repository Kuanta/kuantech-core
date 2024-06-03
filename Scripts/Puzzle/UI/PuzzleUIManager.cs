using Kuantech.Core;

namespace Kuantech.Puzzle.UI
{
    public class PuzzleUIManager : SubManager
    {
        public PuzzleLevelUI LevelUI;

        public override void OnSubmanagersInitialized()
        {
            if(LevelUI != null) LevelUI.Initialize();
        }
        
        public static PuzzleLevelUI GetLevelUI()
        {
            PuzzleUIManager context = GetContext<PuzzleUIManager>();
            if(context == null) return null;
            return context.LevelUI;
        }

        public static void ToggleUI()
        {
            var context = PuzzleUIManager.GetContext<PuzzleUIManager>();
            if(context == null) return;
            if (context.LevelUI == null) return;
            context.LevelUI.gameObject.SetActive(!context.LevelUI.gameObject.activeInHierarchy);
        }
    }
}