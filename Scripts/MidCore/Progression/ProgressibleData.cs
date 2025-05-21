using System;
using Kuantech.Rpg;

namespace Kuantech.Midcore
{
    [Serializable]
    public class ProgressibleData
    {
        public string ParentProgressibleId; //Used for sub upgrades
        public string Id;//Id of the progresable
        public LevelVariable Rank;//Rank of the progressable
    }
}