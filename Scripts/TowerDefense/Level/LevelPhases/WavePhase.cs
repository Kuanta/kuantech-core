using Kuantech.Core;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class WavePhase : LevelPhase
    {
        public override string Key => "WavePhase";
        
        public override void OnEnter(Level level)
        {
            base.OnEnter(level);
            TowerDefenseLevel tdLevel = (ParentLevel as TowerDefenseLevel);
            if (tdLevel == null)
            {
                Debug.LogError("Current level is not tower defense level");
                return;
            }
            tdLevel.SetNextWave();
            (ParentLevel as TowerDefenseLevel)?.ToggleSpawners(true);
        }
    }
}