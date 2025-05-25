using System;
using Kuantech.Rpg;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Core.Combat
{
    [Serializable]
    public class CombatResource
    {
        [FormerlySerializedAs("MaxValueAttribute")] public AttributeAsset maxValueAttributeAsset;
        [FormerlySerializedAs("RegenAttribute")] public AttributeAsset regenAttributeAsset;
        public float CurrentValue;

        public void Remove(float value)
        {
            CurrentValue -= value;
            CurrentValue = Mathf.Max(CurrentValue, 0);
        }
        public void Refresh(StatsModule statsModule)
        {
            CurrentValue = statsModule.GetAttributeValue(maxValueAttributeAsset);
        }
    }
}