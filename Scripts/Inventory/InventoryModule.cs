using System;
using Kuantech.Core;
using UnityEngine;
using Kuantech.Networking;

#if NETWORKING_FISHNET
using FishNet.Connection;
using FishNet.Object;
#endif

namespace Kuantech.Inventory
{
    [Serializable]
    public class InventoryState : ActorModuleSerializableData
    {
        public System.Collections.Generic.List<SerializableItemState> ItemStates;
    }

    public class InventoryModule : ActorModule
    {
        public Equipment Equipment;

        private Inventory _inventory;
        public Inventory Inventory => _inventory;

        // Fires on this module after the shared inventory fires its event (post-network-sync on clients)
        public event Action<Item> OnItemAdded;
        public event Action<Item> OnItemRemoved;
        public event Action<Item, EquipmentSlotType> OnItemEquipped;
        public event Action<Item> OnItemUnequipped;

        public override void Initialize()
        {
            if (Equipment == null)
                Equipment = GetComponent<Equipment>();
            if (Equipment != null)
                Equipment.Initialize(this);
        }

        // ── Inventory attachment ───────────────────────────────────────────────

        public void SetInventory(Inventory inventory)
        {
            DetachInventory();
            inventory.AttachToActor(Actor);
            _inventory = inventory;
            if (_inventory == null) return;

            _inventory.Equipment = Equipment;
            _inventory.OnItemAdded += HandleItemAdded;
            _inventory.OnItemRemoved += HandleItemRemoved;
            _inventory.OnItemEquipped += HandleItemEquipped;
            _inventory.OnItemUnequipped += HandleItemUnequipped;
        }

        public void DetachInventory()
        {
            if (_inventory == null) return;
            _inventory.OnItemAdded -= HandleItemAdded;
            _inventory.OnItemRemoved -= HandleItemRemoved;
            _inventory.OnItemEquipped -= HandleItemEquipped;
            _inventory.OnItemUnequipped -= HandleItemUnequipped;
            _inventory.Detach();
            _inventory = null;
        }

        // ── Event handlers → send RPCs from server, relay events to listeners ─

        private void HandleItemAdded(Item item)
        {
            OnItemAdded?.Invoke(item);
            if (!IsServerInitialized || !IsSpawned) return;
            ObserversOnItemAdded_Rpc(item.GetId(), item.GetAmount(), item.GetInventoryId());
        }

        private void HandleItemRemoved(Item item)
        {
            OnItemRemoved?.Invoke(item);
            if (!IsServerInitialized || !IsSpawned) return;
            ObserversRemoveItem_Rpc(item.GetInventoryId(), 1);
        }

        private void HandleItemEquipped(Item item, EquipmentSlotType slotType)
        {
            OnItemEquipped?.Invoke(item, slotType);
            if (!IsServerInitialized || !IsSpawned) return;
            ObserversEquipItem_Rpc(item.GetInventoryId(), slotType != null ? slotType.Id : "");
        }

        private void HandleItemUnequipped(Item item)
        {
            OnItemUnequipped?.Invoke(item);
            if (!IsServerInitialized || !IsSpawned) return;
            ObserversUnequipItem_Rpc(item.GetInventoryId());
        }

        // ── Public API (delegates to inventory, respects server authority) ─────

        public bool AddItem(ItemDataAsset itemData, int amount = 1)
        {
            if (IsServerInitialized)
                return _inventory?.AddItem(itemData, amount) != null;
            ServerAddItem_Rpc(itemData.GetId(), amount);
            return true;
        }

        public bool AddItemById(string itemId, int amount = 1)
        {
            ItemDataAsset data = ItemsManager.GetItemAsset(itemId);
            return data != null && AddItem(data, amount);
        }

        public void RemoveItem(Item item)
        {
            if (item == null) return;
            if (IsServerInitialized)
                _inventory?.RemoveItem(item);
            else
                ServerRemoveItem_Rpc(item.GetInventoryId(), 1);
        }

        public void EquipItem(Item item, EquipmentSlotType slotType = null)
        {
            if (item == null) return;
            if (IsServerInitialized)
                _inventory?.EquipItem(item, slotType);
            else
                ServerEquipItem_Rpc(item.GetInventoryId(), slotType != null ? slotType.Id : "");
        }

        public void UnequipItem(Item item)
        {
            if (item == null) return;
            if (IsServerInitialized)
                _inventory?.UnequipItem(item);
            else
                ServerUnequipItem_Rpc(item.GetInventoryId());
        }

        public bool AddAndEquipItem(string itemId, EquipmentSlotType slotType = null, int amount = 1)
        {
            ItemDataAsset data = ItemsManager.GetItemAsset(itemId);
            if (data == null) return false;
            if (IsServerInitialized)
            {
                Item item = _inventory?.AddItem(data, amount);
                if (item == null) return false;
                _inventory.EquipItem(item, slotType);
                return true;
            }
            ServerAddAndEquipItem_Rpc(itemId, amount, slotType != null ? slotType.Id : "");
            return true;
        }

        public void ClearInventory() => _inventory?.Clear();

        // ── Queries ───────────────────────────────────────────────────────────

        public Item GetItemAtInventoryId(int id) => _inventory?.GetItemAtSlot(id);
        public Item GetItemById(string id) => _inventory?.GetItemById(id);
        public bool ContainsItemReference(Item item) => _inventory?.Contains(item) ?? false;
        public EquipmentSlotType GetEquipmentSlotTypeFromId(string id) => Equipment?.GetEquipmentSlotType(id);

        // ── Network state sync ────────────────────────────────────────────────

        protected override ActorModuleSerializableData InstantiateState()
        {
            if (_inventory == null) return new InventoryState();
            var data = _inventory.BuildState();
            return new InventoryState { ItemStates = data.ItemStates };
        }

        public override void LoadState(ActorModuleSerializableData serializableData)
        {
            if (_inventory == null || serializableData is not InventoryState state) return;
            _inventory.LoadState(new InventoryData { ItemStates = state.ItemStates });
        }

        public override void OnNetworkSynced()
        {
            if (!KtNetworkManager.IsClient() || Equipment == null) return;
            foreach (var slot in Equipment.slotTable.Values)
            {
                Item item = slot.item;
                if (item == null || item.ItemVisual != null) continue;
                ActorVisual visual = Equipment.GetActorVisual();
                if (visual == null) continue;
                item.ItemVisual = visual.SlotItem(slot.SlotType, item);
            }
        }

        // ── Networking ────────────────────────────────────────────────────────

#if NETWORKING_FISHNET
        [ServerRpc]
        private void ServerAddItem_Rpc(string itemId, int amount)
        {
            ItemDataAsset data = ItemsManager.GetItemAsset(itemId);
            if (data != null) _inventory?.AddItem(data, amount);
        }

        [ServerRpc]
        private void ServerRemoveItem_Rpc(int inventoryId, int amount)
        {
            Item item = _inventory?.GetItemAtSlot(inventoryId);
            if (item != null) _inventory.RemoveItem(inventoryId, amount);
        }

        [ServerRpc]
        private void ServerEquipItem_Rpc(int inventoryId, string slotId)
        {
            Item item = _inventory?.GetItemAtSlot(inventoryId);
            if (item == null) return;
            _inventory.EquipItem(item, Equipment != null ? Equipment.GetEquipmentSlotType(slotId) : null);
        }

        [ServerRpc]
        private void ServerAddAndEquipItem_Rpc(string itemId, int amount, string slotId)
        {
            AddAndEquipItem(itemId, Equipment != null ? Equipment.GetEquipmentSlotType(slotId) : null, amount);
        }

        [ServerRpc]
        private void ServerUnequipItem_Rpc(int inventoryId)
        {
            Item item = _inventory?.GetItemAtSlot(inventoryId);
            if (item != null) _inventory.UnequipItem(item);
        }

        // Server → all observers; skip on server (it already executed)
        [ObserversRpc]
        private void ObserversOnItemAdded_Rpc(string itemId, int amount, int inventoryId)
        {
            if (IsServerInitialized) return;
            ItemDataAsset data = ItemsManager.GetItemAsset(itemId);
            if (data != null) _inventory?.AddItem(data, amount, inventoryId);
        }

        [ObserversRpc]
        private void ObserversRemoveItem_Rpc(int inventoryId, int amount)
        {
            if (IsServerInitialized) return;
            _inventory?.RemoveItem(inventoryId, amount);
        }

        [ObserversRpc]
        private void ObserversEquipItem_Rpc(int inventoryId, string slotId)
        {
            if (IsServerInitialized) return;
            Item item = _inventory?.GetItemAtSlot(inventoryId);
            if (item == null) return;
            _inventory.EquipItem(item, Equipment != null ? Equipment.GetEquipmentSlotType(slotId) : null);
        }

        [ObserversRpc]
        private void ObserversUnequipItem_Rpc(int inventoryId)
        {
            if (IsServerInitialized) return;
            Item item = _inventory?.GetItemAtSlot(inventoryId);
            if (item != null) _inventory.UnequipItem(item);
        }
#else
        private void ServerAddItem_Rpc(string itemId, int amount) { }
        private void ServerRemoveItem_Rpc(int inventoryId, int amount) { }
        private void ServerEquipItem_Rpc(int inventoryId, string slotId) { }
        private void ServerAddAndEquipItem_Rpc(string itemId, int amount, string slotId) { }
        private void ServerUnequipItem_Rpc(int inventoryId) { }
        private void ObserversOnItemAdded_Rpc(string itemId, int amount, int inventoryId) { }
        private void ObserversRemoveItem_Rpc(int inventoryId, int amount) { }
        private void ObserversEquipItem_Rpc(int inventoryId, string slotId) { }
        private void ObserversUnequipItem_Rpc(int inventoryId) { }
#endif
    }
}
