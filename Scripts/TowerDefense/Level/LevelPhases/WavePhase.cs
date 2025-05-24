using Kuantech.Core;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class WavePhase : LevelPhase
    {
        public override string Key => "WavePhase";
        
        public override void OnEnter(Level level)
        {
            Debug.Log("Wave Phase Starting");
        }

        public override void TickPhase(float deltaTime)
        {
      
        }
        public override void OnExit(Level level)
        {
                    
        }
    }
}