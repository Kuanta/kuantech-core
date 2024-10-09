using System;
using Kuantech.Core;

namespace Kuantech.Puzzle
{
    public class PuzzleStateModel : StateModule
    {
        [NonSerialized] public PuzzleLevel CurrentLevel;
        [NonSerialized] public PuzzleLevelState LevelState;

        public void SetCurrentLevel(PuzzleLevel currentLevel)
        {
            CurrentLevel = currentLevel;
        }
        
        public override object GetData()
        {
            return CurrentLevel == null ? null : CurrentLevel.GetLevelState();
        }

        public override void SetData(object loadedData)
        {
            LevelState = loadedData as PuzzleLevelState;
        }

        public override Type GetDataType()
        {
            return typeof(PuzzleStateModel);
        }

        public override void SetDefaultValues()
        {
            LevelState = new PuzzleLevelState();
        }

        public PuzzleLevelState GetLevelState()
        {
            return LevelState;
        }
    }
}