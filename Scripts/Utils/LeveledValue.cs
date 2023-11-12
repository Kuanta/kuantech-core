
using System;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    [Serializable]
    public class LeveledValueFloat
    {
        [SerializeField] private float BaseValue;
        [SerializeField] private float ValuePerLevel;


        public float GetValue(int level = 0)
        {
            return BaseValue + (ValuePerLevel * BaseValue);
        }
    }

    [Serializable]
    public class LeveledValueInt
    {
        [SerializeField] private int BaseValue;
        [SerializeField] private int ValuePerLevel;


        public int GetValue(int level = 0)
        {
            return BaseValue + (ValuePerLevel * level);
        }
    }
}