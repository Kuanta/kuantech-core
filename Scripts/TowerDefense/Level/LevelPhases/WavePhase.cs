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
            Debug.Log("Wave Phase Starting");
            (ParentLevel as TowerDefenseLevel)?.ToggleSpawners(true);
        }
    }
}