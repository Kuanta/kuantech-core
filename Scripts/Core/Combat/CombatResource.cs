using System;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    [Serializable]
    public class CombatResource
    {
        public StatAttribute MaxValueAttribute;
        public StatAttribute RegenAttribute;
        public float CurrentValue;

        public void Remove(float value)
        {
            CurrentValue -= value;
            CurrentValue = Mathf.Max(CurrentValue, 0);
        }
        public void Refresh(StatsModule statsModule)
        {
            CurrentValue = statsModule.GetAttributeValue(MaxValueAttribute);
        }
    }
}