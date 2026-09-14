using Kuantech.Core;

namespace Kuantech.HordeBonkers
{
    public class ToMenuSceneTransitionData : LevelTransitionData
    {
        public int EarnedGold;
    }

    public class ToGameSceneTransitionData : LevelTransitionData
    {
        public string ArenaId;
    }
}