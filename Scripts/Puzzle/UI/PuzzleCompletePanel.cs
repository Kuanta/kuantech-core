using Kuantech.Core;
using Kuantech.UI;
using UnityEngine.UI;

namespace Kuantech.Puzzle.UI
{
    public class PuzzleCompletePanel : UIMenu
    {
        public PuzzleLevelUI ParentUI;
        public Button ContinueButton;

        public void Initialize(PuzzleLevelUI parentUI)
        {
            ContinueButton.onClick.AddListener(()=>{
                LevelManager.GetContext<LevelManager>().CompleteLevel();
            });
        }
    }
}