using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    [Serializable]
    public class PowerLevelArray<T>
    {
        [Serializable]
        public struct PowerLevelArrayElement
        {
            public T Element;
            public int MinLevel;
            public int MaxLevel;
        }
        public List<PowerLevelArrayElement> Elements;
        private List<T> _availableElements;
        private int _currentPowerLevel;
        
        public PowerLevelArray()
        {
            Elements = new List<PowerLevelArrayElement>();
            _availableElements = new List<T>();
        }

        public void SetPowerLevel(int powerLevel)
        {
            _currentPowerLevel = Mathf.Max(0, powerLevel);
            int maxPowerLevel = 0;
            foreach (var element in Elements)
            {
                maxPowerLevel = Mathf.Max(maxPowerLevel, element.MaxLevel);
            }
            
            powerLevel = Mathf.Min(maxPowerLevel, powerLevel); //So that we have at least some content if power level is too high
            _availableElements = new List<T>();
            foreach (var element in Elements)
            {
                if(element.MaxLevel >= powerLevel && element.MinLevel<= powerLevel) _availableElements.Add(element.Element);
            }
        }

        public List<T> GetAvailableElements(int powerLevel)
        {
            SetPowerLevel(powerLevel);
            return _availableElements;
        }

        public List<T> GetAvailableElements()
        {
            return _availableElements ?? GetAvailableElements(_currentPowerLevel);
        }
    }
}