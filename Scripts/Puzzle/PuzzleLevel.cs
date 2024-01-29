using Kuantech.Core;
using Kuantech.Puzzle.UI;

namespace Kuantech.Puzzle
{
    public class PuzzleLevel : Level
    {
        public PuzzleLevelUI LevelUI;
        public override void SetupLevel()
        {
            LevelUI.OnLevelSetup(this);
            base.SetupLevel();
        }
    }
}