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
        public int Amount;
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
        public ItemData Data;

        private ItemStateData _stateData;
        [NonSerialized] public ItemVisual ItemVisual;

        private Dictionary<Type, ItemComponent> _components;

        public Item(ItemData data)
        {
            Data = data;
            _stateData = new ItemStateData
            {
                ItemId = data.GetId(),
                InventoryId = -1,
            };
            _components = new Dictionary<Type, ItemComponent>();
            if (Data.Components == null) return;
            foreach (var def in Data.Components)
            {
                ItemComponent instance = def.CreateInstance();
                instance.Initialize(this);
                _components[instance.GetType()] = instance;
            }

            foreach(var comp in _components.Values)
            {
                comp.OnItemInitialized();
            }
        }

        public Actor GetOwner()
        {
            if (ParentInventory == null) return null;
            return ParentInventory.Owner;
        }

        public int GetLevel()
        {
            if (_stateData == null) return 0;
            return _stateData.ItemLevel;
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
            //Require equipable component
            if (_components.IsNullOrEmpty()) return false;
            if (!HasItemComponent<EquipableComponent>()) return false;
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

        public void SetEquippedSlot(EquipmentSlotType slotType)
        {
            GetItemComponent<EquipableComponent>()?.SetEquippedSlot(slotType);
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

        public bool IsEquipped()
        {
            EquipableComponent equipable = GetItemComponent<EquipableComponent>();
            if (equipable == null) return false;
            return equipable.IsEquipped();
        }

        public EquipmentSlotType GetEquippedSlot()
        {
            return GetItemComponent<EquipableComponent>()?.GetEquippedSlot();
        }

        public string GetEquippedSlotId()
        {
            EquipableComponent equipable = GetItemComponent<EquipableComponent>();
            if (equipable == null) return "";
            return equipable.GetEquippedSlotId();
        }

        public int GetAmount() => _stateData?.Amount ?? 0;

        #endregion

        public static Item GetItemFromData(ItemData data) => new Item(data);

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

            EquipableComponent equipable = GetItemComponent<EquipableComponent>();
            return new SerializableItemState
            {
                ItemDataId      = Data.GetId(),
                InventoryId     = _stateData.InventoryId,
                Amount          = _stateData.Amount,
                ItemLevel       = _stateData.ItemLevel,
                Equipped        = equipable != null && equipable.IsEquipped(),
                EquippedSlotId  = equipable != null ? equipable.GetEquippedSlotId() : "",
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
            ItemData asset = ItemsLibrary.GetItemData(state.ItemDataId);
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
            item.ParentInventory = inventory;
            inventory.Extend(state.InventoryId + 1);
            inventory.Items[state.InventoryId] = item;
            item.LoadComponentStates(state.ComponentStates);

            // Migration: if no component state serialized equipped info, restore from item-level fields
            EquipableComponent equipable = item.GetItemComponent<EquipableComponent>();
            if (equipable != null && !equipable.IsEquipped() && state.Equipped)
                equipable.InitFromLegacyState(state.Equipped, state.EquippedSlotId);

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

        public bool Equip(EquipmentSlotType slotType)
        {
            if (!CanEquip(slotType)) return false;
            foreach (var comp in _components.Values)
                comp.OnItemEquipped(this, slotType);
            // EquipableComponent.OnItemEquipped sets _isEquipped and resolves the slot
            return true;
        }

        public bool Unequip()
        {
            if (!CanUnequip()) return false;
            foreach (var comp in _components.Values)
                comp.OnItemUnequipped(this);
            // EquipableComponent.OnItemUnequipped clears _isEquipped and _equippedSlot
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
                comp.OnAttachedToActor(actor);
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
