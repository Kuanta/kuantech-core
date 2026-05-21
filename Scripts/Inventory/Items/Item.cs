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
        public string EquippedSlotId; // cached after load, before slot type is resolved
    }

    [Serializable]
    public struct ComponentStateEntry
    {
        public string TypeName;
        public string Data;
    }

    [Serializable]
    public struct SerializableItemState
    {
        public string ItemDataId;
        public int InventoryId;
        public int Amount;
        public int ItemLevel;
        public bool Equipped;
        public string EquippedSlotId;
        public List<ComponentStateEntry> ComponentStates;
    }

    [Serializable]
    public class Item
    {
        public Inventory ParentInventory;
        public ItemDataAsset Data;

        private ItemStateData _stateData;
        [NonSerialized] public ItemVisual ItemVisual;

        private Dictionary<Type, ItemComponent> _components;

        public Item(ItemDataAsset data)
        {
            Data = data;
            _stateData = new ItemStateData
            {
                ItemId = data.GetId(),
                InventoryId = -1,
                Equipped = false,
                EquippedSlot = null,
            };
            _components = new Dictionary<Type, ItemComponent>();
            if (Data.Components == null) return;
            foreach (var def in Data.Components)
            {
                ItemComponent instance = def.CreateInstance();
                instance.ParentItem = this;
                _components[instance.GetType()] = instance;
            }
        }

        public Actor GetOwner()
        {
            if (ParentInventory == null) return null;
            return ParentInventory.Owner;
        }

        #region Components

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
            if (_components.IsNullOrEmpty()) return true;
            foreach (var pair in _components)
            {
                if (pair.Value.CanEquipItem(this, slotType) < 0) return false;
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

        public void SetInventoryId(int inventoryId)
        {
            if (_stateData == null) _stateData = new ItemStateData();
            _stateData.InventoryId = inventoryId;
        }

        public void AddAmount(int amount)
        {
            if (_stateData == null) _stateData = new ItemStateData();
            _stateData.Amount = Mathf.Max(0, _stateData.Amount + amount);
        }

        public void SetAmount(int amount)
        {
            if (_stateData == null) _stateData = new ItemStateData();
            _stateData.Amount = amount;
        }

        public void SetEquippedState(bool equipped)
        {
            if (_stateData == null) _stateData = new ItemStateData();
            _stateData.Equipped = equipped;
        }

        public void SetEquippedSlot(EquipmentSlotType slotType)
        {
            if (_stateData == null) _stateData = new ItemStateData();
            _stateData.EquippedSlot = slotType;
            _stateData.EquippedSlotId = slotType != null ? slotType.Id : "";
        }

        #endregion

        #region Getters

        public string GetId() => Data.GetId();
        public string GetName() => Data.GetName();
        public string GetItemId() => Data.GetId();

        public int GetInventoryId()
        {
            if (_stateData == null || (ParentInventory == null)) return -1;
            return _stateData.InventoryId;
        }

        public bool IsEquipped() => _stateData?.Equipped ?? false;

        public EquipmentSlotType GetEquippedSlot() => _stateData?.EquippedSlot;

        public string GetEquippedSlotId()
        {
            if (_stateData == null) return "";
            if (_stateData.EquippedSlot != null) return _stateData.EquippedSlot.Id;
            return _stateData.EquippedSlotId ?? "";
        }

        public int GetAmount() => _stateData?.Amount ?? 0;

        #endregion

        public static Item GetItemFromData(ItemDataAsset data) => new Item(data);

        #region Serialization

        public SerializableItemState BuildState()
        {
            var compStates = new List<ComponentStateEntry>();
            foreach (var comp in _components.Values)
            {
                string state = comp.SerializeState();
                if (state != null)
                    compStates.Add(new ComponentStateEntry { TypeName = comp.GetType().Name, Data = state });
            }

            return new SerializableItemState
            {
                ItemDataId      = Data.GetId(),
                InventoryId     = _stateData.InventoryId,
                Amount          = _stateData.Amount,
                ItemLevel       = _stateData.ItemLevel,
                Equipped        = _stateData.Equipped,
                EquippedSlotId  = GetEquippedSlotId(),
                ComponentStates = compStates.Count > 0 ? compStates : null,
            };
        }

        private void LoadComponentStates(List<ComponentStateEntry> entries)
        {
            if (entries == null) return;
            foreach (var entry in entries)
            {
                foreach (var comp in _components.Values)
                {
                    if (comp.GetType().Name == entry.TypeName)
                    {
                        comp.DeserializeState(entry.Data);
                        break;
                    }
                }
            }
        }

        public static Item FromState(SerializableItemState state, Inventory inventory)
        {
            ItemDataAsset asset = ItemsManager.GetItemAsset(state.ItemDataId);
            if (asset == null)
            {
                Debug.LogWarning($"[Item] FromState: asset '{state.ItemDataId}' not found.");
                return null;
            }
            Item item = new Item(asset);
            item._stateData.ItemId      = state.ItemDataId;
            item._stateData.InventoryId = state.InventoryId;
            item._stateData.Amount      = state.Amount;
            item._stateData.ItemLevel   = state.ItemLevel;
            item._stateData.Equipped       = state.Equipped;
            item._stateData.EquippedSlotId = state.EquippedSlotId ?? "";
            item.ParentInventory = inventory;
            inventory.Extend(state.InventoryId + 1);
            inventory.Items[state.InventoryId] = item;
            item.LoadComponentStates(state.ComponentStates);
            return item;
        }

        #endregion

        #region Operations

        public void OnAdded()
        {
            foreach (var comp in _components.Values)
                comp.OnItemAdded(this);
        }

        public void OnRemoved()
        {
            foreach (var comp in _components.Values)
                comp.OnItemRemoved(this);
        }

        public bool Equip(EquipmentSlotType slotType = null)
        {
            if (!CanEquip(slotType)) return false;
            _stateData.Equipped = true;
            foreach (var comp in _components.Values)
                comp.OnItemEquipped(this, slotType);
            // EquippedSlot is set by EquipableComponent.OnItemEquipped (auto-resolves null slot)
            return true;
        }

        public bool Unequip()
        {
            if (!CanUnequip()) return false;
            foreach (var comp in _components.Values)
                comp.OnItemUnequipped(this);
            _stateData.Equipped = false;
            _stateData.EquippedSlot = null;
            return true;
        }

        public void Use()
        {
            foreach (var comp in _components.Values)
                comp.OnItemUsed(this);
        }

        public void OnAttachedToActor(Actor actor)
        {
            foreach(var comp in _components.Values)
            {
                comp.OnAttachedToActor(actor);
            }
        }

        public void OnDetachedFromActor(Actor actor)
        {
            foreach (var comp in _components.Values)
                comp.OnDetachedFromActor(actor);
        }

        #endregion

        #region Visuals

        public ItemVisual SpawnItemVisual() => SpawnItemVisual(Data.ItemTemplateId);

        public static ItemVisual SpawnItemVisual(string itemVisualId)
        {
            ItemVisual prefab = AssetCollection.GetPrefabByType<ItemVisual>(itemVisualId);
            if (prefab == null) return null;
            return PoolManager.GetObjectFromPool(prefab.gameObject).GetComponent<ItemVisual>();
        }

        #endregion

    }
}
