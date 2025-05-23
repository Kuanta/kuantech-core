using System;

namespace Kuantech.Midcore
{
    /// <summary>
    /// A data structure representing the progression of a level in a game.
    /// </summary>
    [Serializable]
    public struct LevelProgressionData
    {
        public int WorldIndex;
        public int LevelIndex;
        public int LevelScore;
    }
}