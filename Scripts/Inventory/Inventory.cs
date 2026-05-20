using System;
using System.Collections.Generic;
using UnityEngine;
using Kuantech.Core;

namespace Kuantech.Inventory
{
    [Serializable]
    public class InventoryData
    {
        public List<SerializableItemState> ItemStates = new();
    }

    public class Inventory : ISaveable
    {
        public Item[] Items { get; private set; }
        public int Capacity => Items.Length;

        public event Action<Item> OnItemAdded;
        public event Action<Item> OnItemRemoved;
        public event Action<Item, EquipmentSlotType> OnItemEquipped;
        public event Action<Item> OnItemUnequipped;

        //Runtime
        [NonSerialized] public Actor Owner;

        public Inventory(int size)
        {
            Items = new Item[size];
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public Equipment GetEquipment()
        {
            return null;
        }
        
        public Item GetItemAtSlot(int slot)
        {
            if (slot < 0 || slot >= Items.Length) return null;
            return Items[slot];
        }

        public Item GetItemById(string itemId)
        {
            foreach (var item in Items)
                if (item != null && item.GetId() == itemId) return item;
            return null;
        }

        public bool Contains(Item item)
        {
            foreach (var i in Items)
                if (i == item) return true;
            return false;
        }

        public int GetAvailableSlot()
        {
            for (int i = 0; i < Items.Length; i++)
                if (Items[i] == null) return i;
            return -1;
        }

        public bool CanAddItem(ItemDataAsset data) => GetAvailableSlot() >= 0;

        public List<Item> GetAllItems()
        {
            var result = new List<Item>();
            foreach (var item in Items)
                if (item != null) result.Add(item);
            return result;
        }

        public List<T> GetItemComponents<T>() where T : ItemComponent
        {
            var result = new List<T>();
            foreach (var item in Items)
            {
                if (item == null) continue;
                T comp = item.GetItemComponent<T>();
                if (comp != null) result.Add(comp);
            }
            return result;
        }

        public List<(Item item, T comp)> GetItemsWithComponent<T>() where T : ItemComponent
        {
            var result = new List<(Item, T)>();
            foreach (var item in Items)
            {
                if (item == null) continue;
                T comp = item.GetItemComponent<T>();
                if (comp != null) result.Add((item, comp));
            }
            return result;
        }

        // ── Add ───────────────────────────────────────────────────────────────

        public Item AddItem(ItemDataAsset data, int amount = 1, int slot = -1)
        {
            if (data == null) return null;
            amount = Mathf.Max(1, amount);

            if (data.stackable)
            {
                Item existing = GetItemById(data.GetId());
                if (existing != null)
                {
                    existing.AddAmount(amount);
                    OnItemAdded?.Invoke(existing);
                    return existing;
                }
            }

            int target = slot >= 0 ? slot : GetAvailableSlot();
            if (target < 0) return null;
            if (target >= Items.Length) Extend(target + 1);
            if (Items[target] != null) return null;

            Item item = Item.GetItemFromData(data);
            item.SetInventoryId(target);
            item.ParentInventory = this;
            Items[target] = item;
            item.OnAdded();
            OnItemAdded?.Invoke(item);
            return item;
        }

        public Item AddItem(string itemId, int amount = 1)
        {
            ItemDataAsset data = ItemsManager.GetItemAsset(itemId);
            return data != null ? AddItem(data, amount) : null;
        }

        // ── Remove ────────────────────────────────────────────────────────────

        public void RemoveItem(Item item)
        {
            if (item == null || item.ParentInventory != this) return;
            RemoveItem(item.GetInventoryId());
        }

        public void RemoveItem(int slot, int amount = 1)
        {
            Item item = GetItemAtSlot(slot);
            if (item == null) return;

            if (item.Data.stackable)
            {
                int remaining = item.GetAmount() - amount;
                if (remaining > 0) { item.SetAmount(remaining); return; }
            }

            TryUnequipItem(item);
            Items[slot] = null;
            item.OnRemoved();
            OnItemRemoved?.Invoke(item);
        }

        private void TryUnequipItem(Item item)
        {
            if (item.IsEquipped()) UnequipItem(item);
        }

        // ── Equip ─────────────────────────────────────────────────────────────

        public bool EquipItem(Item item, EquipmentSlotType slotType = null)
        {
            if (item == null) return false;
            if (!Contains(item) && AddItem(item.Data) == null) return false;
            if (!item.CanEquip(slotType)) return false;
            item.Equip(slotType);
            OnItemEquipped?.Invoke(item, slotType);
            return true;
        }

        public bool UnequipItem(Item item)
        {
            if (item == null || !item.CanUnequip()) return false;
            item.Unequip();
            OnItemUnequipped?.Invoke(item);
            return true;
        }

        public bool AddAndEquipItem(ItemDataAsset data, EquipmentSlotType slotType, int amount = 1)
        {
            Item item = AddItem(data, amount);
            return item != null && EquipItem(item, slotType);
        }

        // ── Utility ───────────────────────────────────────────────────────────

        public void Clear()
        {
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i] == null) continue;
                TryUnequipItem(Items[i]);
                Items[i].OnRemoved();
                OnItemRemoved?.Invoke(Items[i]);
                Items[i] = null;
            }
        }

        public void SwapItems(int slotA, int slotB)
        {
            if (slotA < 0 || slotA >= Items.Length || slotB < 0 || slotB >= Items.Length) return;
            Item a = Items[slotA];
            Item b = Items[slotB];
            Items[slotA] = b;
            Items[slotB] = a;
            a?.SetInventoryId(slotB);
            b?.SetInventoryId(slotA);
        }

        public void Extend(int newSize)
        {
            if (newSize <= Items.Length) return;
            var newItems = new Item[newSize];
            Array.Copy(Items, 0, newItems, 0, Items.Length);
            Items = newItems;
        }

        // ── Serialization ─────────────────────────────────────────────────────

        public InventoryData BuildState()
        {
            var states = new List<SerializableItemState>();
            foreach (var item in Items)
                if (item != null) states.Add(item.BuildState());
            return new InventoryData { ItemStates = states };
        }

        public void LoadState(InventoryData data)
        {
            if (data?.ItemStates == null) return;
            foreach (var state in data.ItemStates)
            {
                if (GetItemAtSlot(state.InventoryId) != null) continue;
                Item.FromState(state, this);
            }
        }

        #region Save & Load

        public byte[] Serialize() => SaveUtility.SerializePoco(BuildState());

        public void Deserialize(byte[] data)
        {
            LoadState(SaveUtility.DeserializePoco<InventoryData>(data));
        }

        #endregion

        #region Attaching
        public void AttachToActor(Actor actor)
        {
            Owner = actor;
            foreach (var item in Items)
            {
                if (item == null) continue;
                item.OnAttachedToActor(actor);
            }
        }

        public void Detach()
        {
            Actor actor = Owner;
            Owner = null;
            if (actor == null) return;
            foreach (var item in Items)
            {
                if (item == null) continue;
                item.OnDetachedFromActor(actor);
            }
        }
        #endregion
    }
}
