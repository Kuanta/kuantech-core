using Kuantech.UI;
using UnityEngine.UI;

namespace Kuantech.Puzzle.UI
{
    public class PuzzleFailPanel : UIMenu
    {
        public Button RestartButton;

        public void Initialize(PuzzleLevelUI parentUI)
        {
            RestartButton.onClick.AddListener(() =>
            {
                parentUI.CurrentLevel.RestartLevel();
            });
        }

    }
}