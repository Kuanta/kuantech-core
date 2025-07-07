using System.Collections;
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

        private IEnumerator _currentRoutine = null;
        public void ChangePhase(LevelPhase newPhase, float changeDelay = 0f)
        {
            if (_currentRoutine != null)
            {
                ParentLevel.StopCoroutine(_currentRoutine);
                _currentRoutine = null;
            }

            _currentRoutine = ChangePhaseRoutine(newPhase, changeDelay);
            ParentLevel.StartCoroutine(_currentRoutine);
        }

        private IEnumerator ChangePhaseRoutine(LevelPhase newPhase, float changeDelay)
        {
            yield return new WaitForSeconds(changeDelay);
            var oldPhase = CurrentPhase;
            oldPhase?.OnExit(ParentLevel);

            CurrentPhase = newPhase;
            CurrentPhase.OnEnter(ParentLevel);
           
            ParentLevel.OnLevelPhaseChange(oldPhase, newPhase);

            _currentRoutine = null;
        }
        
        public void ChangePhase(string phaseKey, float delay=0.0f)
        {
            if (!_phases.TryGetValue(phaseKey, out var newPhase))
            {
                Debug.LogError($"Phase '{phaseKey}' is not registered.");
                return;
            }
            ChangePhase(newPhase, delay);
        }

        public void Reset()
        {
            _phases.Clear();
            CurrentPhase = null;
        }
    }
}