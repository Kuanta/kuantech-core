
using System;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    [Serializable]
    public class LeveledValueFloat
    {
        [SerializeField] private float BaseValue;
        [SerializeField] private float ValuePerLevel;
        [SerializeField] private int LevelIntervals;

        public float GetValue(int level = 0)
        {
            if (LevelIntervals > 0) level = Mathf.FloorToInt(level / LevelIntervals);
            return BaseValue + (ValuePerLevel * level);
        }
    }

    [Serializable]
    public class LeveledValueInt
    {
        [SerializeField] private float BaseValue;
        [SerializeField] private float ValuePerLevel;
        [SerializeField] private int LevelIntervals = 0;

        public int GetValue(int level = 0)
        {
            if(LevelIntervals > 0) level = Mathf.FloorToInt(level / LevelIntervals);
            return Mathf.FloorToInt(BaseValue + (ValuePerLevel * level));
        }
    }
}