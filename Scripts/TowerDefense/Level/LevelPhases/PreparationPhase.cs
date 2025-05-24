using Kuantech.Core;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class PreparationPhase : LevelPhase
    {
        public override string Key => "Preparation Phase";
        public float PreparationTime = -1;
        public PreparationPhase()
        {
            // Constructor logic if needed
        }
        public override void OnEnter(Level level)
        {
            base.OnEnter(level);
            Debug.Log("Preperation Phase Starting");
        }

        public override void TickPhase(float deltaTime)
        {
            if(PreparationTime > 0 && GetPhaseDuration() >= PreparationTime)
            {
                // End preparation phase
                var towerDefenseLevel = (TowerDefenseLevel) ParentLevel;
                towerDefenseLevel.OnPreperationPhaseEnd();
            }
      
        }
        public override void OnExit(Level level)
        {
            base.OnExit(level);
        }
    }
}