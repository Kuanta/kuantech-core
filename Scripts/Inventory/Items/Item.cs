using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Inventory
{
    [Serializable]
    public class ItemStateData
    {
        public string ItemId;  
        public int InventoryId;
        public int ItemLevel;
        public bool Equipped;
        public int Amount;
        public EquipmentSlotType EquippedSlot;
    }

    /// <summary>
    /// Compact, ScriptableObject-free snapshot of an item. Safe to send over RPCs and save to disk.
    /// </summary>
    [Serializable]
    public struct SerializableItemState
    {
        public string ItemDataId;
        public int InventoryId;
        public int Amount;
        public int ItemLevel;
        public bool Equipped;
        public string EquippedSlotId;
    }

    [Serializable]
    public class Item
    {
        public InventoryModule ParentInvetory;
        public ItemData Data;
        
        //Stats
        private ItemStateData _stateData;
        //public EquipmentSlotType CurrentSlot;
        [NonSerialized] public ItemVisual ItemVisual;

        //Comps
        private Dictionary<Type, ItemComponent> _components;
        
        public Item(ItemData data)
        {
            Data = data;
            CreateStateData();
            _components = new Dictionary<Type, ItemComponent>();
            if(Data.Components == null) return;
            foreach(var component in Data.Components)
            {
                _components[component.GetType()] = component; //Is this polymorphic?
            }
        }

        private void CreateStateData()
        {
            _stateData = new ItemStateData();
            _stateData.ItemId = Data.Id;
            _stateData.InventoryId = -1;
            _stateData.Equipped = false;
            _stateData.EquippedSlot = null;
        }

        public void SetStateData(ItemStateData stateData)
        {
            _stateData = stateData;
        }

        #region Item Data Components
        public T GetItemComponent<T>() where T : ItemComponent
        {
            if (_components.TryGetValue(typeof(T), out ItemComponent component))
                return component as T;
            return null;
        }

        public bool HasItemComponent<T>() where T : ItemComponent
        {
            return _components.ContainsKey(typeof(T));
        }
        #endregion

        #region Checks

        
        public bool CanEquip(EquipmentSlotType slotType)
        {
            if(_components.IsNullOrEmpty()) return true;
            foreach(var pair in _components)
            {
                if(!pair.Value.CanEquipItem(this, slotType)) return false;
            }
            return true;
        }

        public bool CanUnequip()
        {
            if (_components.IsNullOrEmpty()) return true;
            foreach (var pair in _components)
            {
                if (!pair.Value.CanUnequipItem(this)) return false;
            }
            return true;
        }
        #endregion

        #region Setters
        public void SetParentInventory(InventoryModule inventory)
        {
            ParentInvetory = inventory;
        }

        public void SetInventoryId(int inventoryId)
        {
            if(_stateData == null) _stateData = new ItemStateData();
            _stateData.InventoryId = inventoryId;
        }

        public void AddAmount(int amount)
        {
            if (_stateData == null) _stateData = new ItemStateData();
            _stateData.Amount += amount;
            _stateData.Amount = Mathf.Max(0, _stateData.Amount);
        }
        public void SetAmount(int amount)
        {
            if (_stateData == null) _stateData = new ItemStateData();
            _stateData.Amount = amount;
        }

        public void SetEquippedState(bool equipped)
        {
            if(_stateData == null) CreateStateData();
            _stateData.Equipped = equipped;
        }

        public void SetEquippedSlot(EquipmentSlotType slotType)
        {
            if (_stateData == null) CreateStateData();
            _stateData.EquippedSlot = slotType;
        }

        #endregion

        #region Getters
        /// <summary>
        /// Returns the item id
        /// </summary>
        /// <returns></returns>
        public string GetId()
        {
            return Data.Id;
        }
        
        public int GetInventoryId()
        {
            if (_stateData == null || ParentInvetory == null) return -1;
            return _stateData.InventoryId;
        }
        public bool IsEquipped()
        {
            if (_stateData == null) return false;
            return _stateData.Equipped;
        }
        public EquipmentSlotType GetEquippedSlot()
        {
            if(_stateData == null) return null;
            return _stateData.EquippedSlot;
        }

        public string GetEquippedSlotId()
        {
            if (_stateData == null || _stateData.EquippedSlot == null) return "";
            return _stateData.EquippedSlot.Id;
        }

        public int GetAmount()
        {
            if (_stateData == null) return 0;
            return _stateData.Amount;
        }

        /// <summary>
        /// Returns the item name
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            string name = Data.Name;
            return name;
        }

        public string GetItemId()
        {
            return Data.Id;
        }
        #endregion
       
        public static Item GetItemFromData(ItemData data)
        {
            return new Item(data);
        }

        #region Serialization

        public SerializableItemState BuildState()
        {
            return new SerializableItemState
            {
                ItemDataId     = Data.Id,
                InventoryId    = _stateData.InventoryId,
                Amount         = _stateData.Amount,
                ItemLevel      = _stateData.ItemLevel,
                Equipped       = _stateData.Equipped,
                EquippedSlotId = GetEquippedSlotId(),
            };
        }

        /// <summary>
        /// Reconstructs an Item from a serialized state. Places it in the inventory's items array.
        /// Returns null if ItemData is not found.
        /// </summary>
        public static Item FromState(SerializableItemState state, InventoryModule inventory)
        {
            ItemData itemData = ItemsManager.GetItemData(state.ItemDataId);
            if (itemData == null)
            {
                Debug.LogWarning($"[Item] FromState: ItemData '{state.ItemDataId}' not found.");
                return null;
            }
            Item item = new Item(itemData);
            item.SetAmount(state.Amount);
            item._stateData.ItemId      = state.ItemDataId;
            item._stateData.InventoryId = state.InventoryId;
            item._stateData.ItemLevel   = state.ItemLevel;
            item._stateData.Equipped    = state.Equipped;
            item.SetEquippedSlot(inventory.GetEquipmentSlotTypeFromId(state.EquippedSlotId));
            item.ParentInvetory        = inventory;
            inventory.ExtendInventory(state.InventoryId + 1);
            inventory.items[state.InventoryId] = item;
            return item;
        }

        #endregion

        #region Operations
        /// <summary>
        /// Called when item added
        /// </summary>
        public void OnAdded()
        {
            foreach(var comp in _components.Values)
            {
                comp.OnItemAdded(this);
            }
        }

        /// <summary>
        /// Called when item removed
        /// </summary>
        public void OnRemoved()
        {
            foreach (var comp in _components.Values)
            {
                comp.OnItemRemoved(this);
            }
        }

        /// <summary>
        /// Equips the item
        /// </summary>
        public bool Equip(EquipmentSlotType slotType=null)
        {
            if(!CanEquip(slotType)) return false;

            foreach (var comp in _components.Values)
            {
                comp.OnItemEquipped(this, slotType);
            }
            _stateData.Equipped = true;
            _stateData.EquippedSlot = slotType;
            return true;
        }

        /// <summary>
        /// Unequips the item
        /// </summary>
        public bool Unequip()
        {
            if(!CanUnequip()) return false;

            foreach (var comp in _components.Values)
            {
                comp.OnItemUnequipped(this);
            }
            _stateData.Equipped = false;
            _stateData.EquippedSlot = null;
            return true;
        }

        /// <summary>
        /// Uses the item
        /// </summary>
        public void Use()
        {
            foreach (var comp in _components.Values)
            {
                comp.OnItemUsed(this);
            }
        }
        #endregion

        #region Visuals

        public ItemVisual SpawnItemVisual()
        {
            return SpawnItemVisual(Data.ItemTemplateId);
        }

        public static ItemVisual SpawnItemVisual(string itemVisualId)
        {
            ItemVisual itemVisualPrefab = AssetCollection.GetPrefabByType<ItemVisual>(itemVisualId);
            if (itemVisualPrefab == null) return null;
            return PoolManager.GetObjectFromPool(itemVisualPrefab.gameObject).GetComponent<ItemVisual>();
        }

        #endregion
    }
}