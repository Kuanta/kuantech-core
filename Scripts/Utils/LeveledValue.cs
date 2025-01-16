
using System;
using UnityEngine;

namespace Kuantech.Utils
{
    [Serializable]
    public class LeveledValueFloat
    {
        [SerializeField] private float BaseValue;
        [SerializeField] private float ValuePerLevel;
        [SerializeField] private int LevelIntervals;
        [Header("Limits")]
        [SerializeField] private float MinValue;
        [SerializeField] private float MaxValue;
        [SerializeField] private bool LimitValue;

        public float GetValue(int level = 0)
        {
            if (LevelIntervals > 0) level = Mathf.FloorToInt(level / LevelIntervals);
            float value = BaseValue + (ValuePerLevel * level);
            if (LimitValue)
            {
                return Mathf.Clamp(value, MinValue, MaxValue);
            }
            return value;
        }
    }

    [Serializable]
    public class LeveledValueInt
    {
        [SerializeField] private float BaseValue;
        [SerializeField] private float ValuePerLevel;
        [SerializeField] private int LevelIntervals = 0;
        [Header("Limits")]
        [SerializeField] private int MinValue;
        [SerializeField] private int MaxValue;
        [SerializeField] private bool LimitValue;

        public int GetValue(int level = 0)
        {
            if(LevelIntervals > 0) level = Mathf.FloorToInt(level / LevelIntervals);
            int value = Mathf.FloorToInt(BaseValue + (ValuePerLevel * level));
            if(LimitValue)
            {
                return Mathf.Clamp(value, MinValue, MaxValue);
            }
            return value;
        }
    }
}