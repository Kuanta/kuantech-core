using System;
using System.Collections.Generic;

namespace Kuantech.Puzzle
{
    [Serializable]
    public class PuzzleLevelState
    {
        public Dictionary<int, byte[]> LevelElementStates;
    }
}