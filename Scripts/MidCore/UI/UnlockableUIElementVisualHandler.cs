using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    [Serializable]
    public struct StateVisualEntry
    {
        public UnlockableStates State;
        public GameObject Visual;
    }
    public class UnlockableUIElementVisualHandler : MonoBehaviour
    {
        public List<StateVisualEntry> StateVisuals = new List<StateVisualEntry>();
        private Dictionary<UnlockableStates, GameObject> _stateVisualsDictionary = new Dictionary<UnlockableStates, GameObject>();

        public void SetVisuals()
        {
            foreach (var stateEntry in StateVisuals)
            {
                _stateVisualsDictionary[stateEntry.State] = stateEntry.Visual;
            }  
        }
        
        public void SetVisual(UnlockableStates state)
        {
            foreach (var pair in _stateVisualsDictionary)
            {
                pair.Value.SetActive(state == pair.Key);
            }
        }
    }
}