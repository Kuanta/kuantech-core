using System;
using System.Collections.Generic;

namespace Kuantech.Puzzle
{
    /// <summary>
    /// Serializable state for puzzle condition stage
    /// </summary>
    [Serializable]
    public struct StageState
    {
        public List<string> TargetKeys;
        public List<int> CollectedAmounts;
        public int CurrentFlatScore;
    }
    
    /// <summary>
    /// Serializable state for win condition tracker
    /// </summary>
    [Serializable]
    public class WinConditionTrackerState
    {
        public int CurrentStageIndex;
        public StageState CurrentStageState;
    }
}