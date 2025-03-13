using System;
using System.Collections.Generic;
using Kuantech.Rpg.Inventory;
using Kuantech.Utils;
using Unity.VisualScripting;

namespace Kuantech.Core
{
    [Serializable]
    public struct ActorVisualPartEntry
    {
        public EquipmentSlotType SlotType;
        public List<ActorVisualPart> VisualParts;
    }
    
    [Serializable]
    public class ActorVisualPartsCollection
    {
        public List<ActorVisualPartEntry> VisualPartEntries;
    }

    [Serializable]
    public class ActorVisualPartsHandler
    {
        public ActorVisualPartsCollection PartsCollection;
        private Dictionary<EquipmentSlotType, List<ActorVisualPart>> _visualPartsBySlots;
        private Dictionary<EquipmentSlotType, int> _currentVisualParts;

        public void Initialize()
        {
            _visualPartsBySlots = new Dictionary<EquipmentSlotType, List<ActorVisualPart>>();
            _currentVisualParts = new Dictionary<EquipmentSlotType, int>();
            
            foreach (var entry in PartsCollection.VisualPartEntries)
            {
                if(entry.VisualParts.IsNullOrEmpty()) continue;
                if (!_visualPartsBySlots.ContainsKey(entry.SlotType))
                {
                    _visualPartsBySlots[entry.SlotType] = new List<ActorVisualPart>();
                }

                foreach (var part in entry.VisualParts)
                {
                    _visualPartsBySlots[entry.SlotType].Add(part);
                }

                _currentVisualParts[entry.SlotType] = 0;
            }
        }
        
        /// <summary>
        /// Toggles the actor visual part
        /// </summary>
        /// <param name="slotType"></param>
        /// <param name="toggle"></param>
        public void ToggleVisualPart(EquipmentSlotType slotType, bool toggle)
        {
            ActorVisualPart part = GetActiveVisualPart(slotType);
            if (part == null) return;
            part.gameObject.SetActive(toggle);
        }
        
        /// <summary>
        /// Returns the active visual part
        /// </summary>
        /// <param name="slotType"></param>
        /// <returns></returns>
        public ActorVisualPart GetActiveVisualPart(EquipmentSlotType slotType)
        {
            if (_visualPartsBySlots == null) return null;
            if (!_visualPartsBySlots.ContainsKey(slotType)) return null;
            int activePartIndexForSlot = GetIndexForActiveVisualPart(slotType);
            return _visualPartsBySlots[slotType][activePartIndexForSlot];
        }
        
        /// <summary>
        /// Returns the active index for visual part. Like the index of active hair, beard, etc.
        /// </summary>
        /// <param name="slotType"></param>
        /// <returns></returns>
        public int GetIndexForActiveVisualPart(EquipmentSlotType slotType)
        {
            if (_currentVisualParts == null) return -1;
            if (!_currentVisualParts.ContainsKey(slotType)) return -1;
            return _currentVisualParts[slotType];
        }
        /// <summary>
        /// Toggles actor part by given index
        /// </summary>
        /// <param name="slotType"></param>
        /// <param name="index"></param>
        public void SetActiveVisualPart(EquipmentSlotType slotType, int index)
        {
            if (!_visualPartsBySlots.ContainsKey(slotType)) return;
            index = index % _visualPartsBySlots[slotType].Count;
            for (int i = 0; i < _visualPartsBySlots[slotType].Count; ++i)
            {
                _visualPartsBySlots[slotType][i].Toggle(i == index);
            }
        }
        
        /// <summary>
        /// Toggles all active visual parts
        /// </summary>
        public void RefreshVisualParts()
        {
            foreach (var pair in _currentVisualParts)
            {
                SetActiveVisualPart(pair.Key, pair.Value);
            }
        }
    }
}