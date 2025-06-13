using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core
{
    public class LevelPhaseSystem
    {
        public Level ParentLevel;
        public LevelPhase CurrentPhase { get; private set; }
        public string CurrentPhaseKey => CurrentPhase?.Key;

        private readonly Dictionary<string, LevelPhase> _phases = new();
        
        public LevelPhaseSystem(Level parentLevel)
        {
            ParentLevel = parentLevel;
        }
        
        public void RegisterPhase(LevelPhase phase)
        {
            phase.ParentLevel = ParentLevel;
            if (!_phases.ContainsKey(phase.Key))
                _phases.Add(phase.Key, phase);
        }

        public void ChangePhase(LevelPhase newPhase)
        {
            var oldPhase = CurrentPhase;
            oldPhase?.OnExit(ParentLevel);

            CurrentPhase = newPhase;
            CurrentPhase.OnEnter(ParentLevel);
           
            ParentLevel.OnLevelPhaseChange(oldPhase, newPhase);
        }
        
        public void ChangePhase(string phaseKey)
        {
            if (!_phases.TryGetValue(phaseKey, out var newPhase))
            {
                Debug.LogError($"Phase '{phaseKey}' is not registered.");
                return;
            }
            ChangePhase(newPhase);
        }

        public void Reset()
        {
            _phases.Clear();
            CurrentPhase = null;
        }
    }
}