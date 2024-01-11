using System;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    [Serializable]
    public abstract class StatusEffect
    {
        public CombatModule Target;
        public float Duration;
        public float TickPeriod;
        
        public float ApplyTime;
        public float LastTickTime;
        public bool ToBeRemoved;
        
        public virtual void Init(CombatModule actor)
        {
            Target = actor;
        }

        public virtual void OnAdd()
        {
            ApplyTime = Time.time;
            LastTickTime = Time.time;
        }
        
        public virtual void OnTick()
        {
            
        }

        public bool IsExpired()
        {
            return (Time.time - ApplyTime > Duration && Duration >= 0);
        }
        public virtual void OnRemove()
        {
            
        }
    }
}