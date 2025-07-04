using UnityEngine;

namespace Kuantech.Core
{
    public abstract class LevelPhase
    {
       public abstract string Key { get; }

       public Level ParentLevel;
       
       protected float PhaseStartTime;

       public virtual void OnEnter(Level level)
       {
           PhaseStartTime = Time.time;
       }
       public virtual void TickPhase(float deltaTime){}
       
       public virtual void OnExit(Level level) { }

       public void CompletePhase()
       {
            
       }
       
       public float GetPhaseDuration()
       {
           return Time.time - PhaseStartTime;
       }
    }
}