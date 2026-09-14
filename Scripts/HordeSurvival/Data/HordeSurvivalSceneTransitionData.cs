using Kuantech.Core;

namespace Kuantech.HordeSurvival
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