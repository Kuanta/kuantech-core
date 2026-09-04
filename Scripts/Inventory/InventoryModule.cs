using System;
using Kuantech.Core;
using Kuantech.Networking;
using UnityEngine;

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
        [Header("Setup")]
        [SerializeField] private int Capacity = 20;
        // Slot list only -- EquipmentSlot.item is runtime state and stays empty on the prefab/asset.
        [SerializeField] private Equipment EquipmentSetup;

        private Inventory _inventory;
        public Inventory Inventory => _inventory;

        // Tracks which ActorVisual equipped visuals currently live on, so RefreshEquippedVisuals can tell
        // "item moved to a new mesh" (e.g. PlayerModule's FP/TP toggle) apart from "already up to date" and
        // properly tear down the stale instance instead of leaking it under the now-inactive old mesh.
        private ActorVisual _lastEquipVisual;

        // Fires on this module after the shared inventory fires its event (post-network-sync on clients)
        public event Action<Item> OnItemAdded;
        public event Action<Item> OnItemRemoved;
        public event Action<Item, EquipmentSlotType> OnItemEquipped;
        public event Action<Item> OnItemUnequipped;
        // Fires when an Inventory is bound/unbound wholesale (SetInventory/DetachInventory) -- distinct from
        // OnItemEquipped, which only fires for items that go through Inventory.EquipItem. Items already
        // sitting in Equipment.slotTable when the inventory is attached (e.g. restored from saved state via
        // RestoreEquipmentState, which calls Equipment.EquipItem directly) never raise OnItemEquipped, so a
        // listener that needs to know about every currently-equipped item (e.g. EffectsModule registering
        // weapon FX sockets) must react to this instead and walk GetEquippedItems() itself.
        public event Action<Inventory> OnInventoryAttached;
        public event Action<Inventory> OnInventoryDetached;

        // ── Inventory attachment ───────────────────────────────────────────────

        public override void Initialize()
        {
            base.Initialize();
            if (Actor.VisualHandler != null)
            {
                //Subscribe to post actor visual set. New slots from new visual must be set in slots handler
                Actor.VisualHandler.OnPostActorVisualSet += OnActorVisualSet;
            }

            // Every actor with this module owns its own Inventory -- nothing outside builds one for it.
            // LoadState (called right after this, when a saved state exists) needs _inventory non-null.
            if (_inventory == null)
            {
                var inventory = new Inventory(Capacity) { Equipment = EquipmentSetup };
                inventory.Initialize(Capacity);
                SetInventory(inventory);
            }
        }

        public void SetInventory(Inventory inventory)
        {
            DetachInventory();
            _inventory = inventory;
            if (_inventory == null) return;

            var eq = _inventory.Equipment;
            if (eq != null)
            {
                eq.OnItemSlotted += HandleItemSlotted;
                eq.OnItemUnslotted += HandleItemUnslotted;
            }

            _inventory.OnItemAdded += HandleItemAdded;
            _inventory.OnItemRemoved += HandleItemRemoved;
            _inventory.OnItemEquipped += HandleItemEquipped;
            _inventory.OnItemUnequipped += HandleItemUnequipped;
            _inventory.AttachToActor(Actor);
            RefreshEquippedVisuals();

            OnInventoryAttached?.Invoke(_inventory);
        }

        public void DetachInventory()
        {
            if (_inventory == null) return;

            // Fire before anything is unhooked/despawned below -- listeners (EffectsModule) need to walk
            // GetEquippedItems() and their still-valid ItemVisual references to unregister per-item state.
            OnInventoryDetached?.Invoke(_inventory);

            var eq = _inventory.Equipment;
            if (eq != null)
            {
                eq.OnItemSlotted -= HandleItemSlotted;
                eq.OnItemUnslotted -= HandleItemUnslotted;
                // Remove equipped item visuals from this actor WITHOUT changing the loadout. Detaching an
                // inventory from an actor must never unequip its items — equipped state is persistent
                // inventory data, and firing equip/unequip here re-enters SetInventory (infinite loop).
                foreach (var slot in eq.slotTable.Values)
                    RemoveItemVisual(slot.item);
            }

            _inventory.OnItemAdded -= HandleItemAdded;
            _inventory.OnItemRemoved -= HandleItemRemoved;
            _inventory.OnItemEquipped -= HandleItemEquipped;
            _inventory.OnItemUnequipped -= HandleItemUnequipped;
            _inventory.Detach(); // removes each equipped item's actor-side effects (stat modifiers)
            _inventory = null;
            _lastEquipVisual = null;
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
            // Equipment sync is handled by Inventory.EquipItem
            OnItemEquipped?.Invoke(item, slotType);
            if (!IsServerInitialized || !IsSpawned) return;
            ObserversEquipItem_Rpc(item.GetInventoryId(), item.GetEquippedSlotId());
        }

        private void HandleItemUnequipped(Item item)
        {
            // Equipment sync is handled by Inventory.UnequipItem
            OnItemUnequipped?.Invoke(item);
            if (!IsServerInitialized || !IsSpawned) return;
            ObserversUnequipItem_Rpc(item.GetInventoryId());
        }

        private void OnActorVisualSet(ActorVisual visual) => RefreshEquippedVisuals();

        private void RefreshEquippedVisuals()
        {
            if (_inventory?.Equipment?.slotTable == null) return;
            ActorVisual visual = Actor.VisualHandler != null ? Actor.VisualHandler.GetActorVisual() : null;
            if (visual == null) return;

            ActorVisual previousVisual = _lastEquipVisual;
            _lastEquipVisual = visual;

            foreach (var slot in _inventory.Equipment.slotTable.Values)
            {
                Item item = slot.item;
                if (item == null) continue;

                // item.ItemVisual is shared state on the (possibly persistent, cross-actor) Item, not scoped
                // to this actor -- if it still points at a visual parented under a DIFFERENT actor (e.g. the
                // character-preview actor never got a clean detach before this one attached the same
                // inventory), that reference is stale for us specifically. Treat it as absent and re-slot.
                if (item.ItemVisual != null && item.ItemVisual.transform.IsChildOf(visual.transform)) continue;

                // The active ActorVisual changed under us (e.g. PlayerModule switching FP/TP mesh) rather
                // than this being a first-time equip -- properly unslot from the mesh we just left, or the
                // old instance leaks as an invisible orphan under the now-deactivated mesh, still subscribed
                // to whatever it was subscribed to (e.g. WeaponVisual/CombatModule).
                if (item.ItemVisual != null)
                {
                    if (previousVisual != null) previousVisual.UnslotItem(item);
                    else { item.ItemVisual.OnUnequipped(); item.ItemVisual.Despawn(); item.ItemVisual = null; }
                }

                item.ItemVisual = visual.EquipItemVisual(item);
            }
        }

        private void HandleItemSlotted(Item item, EquipmentSlotType slotType)
        {
            if (Actor.VisualHandler == null)
            {
                Debug.LogWarning($"[InventoryModule] HandleItemSlotted: '{item.GetId()}' -- Actor has no ActorVisualHandler on '{name}'.");
                return;
            }
            ActorVisual visual = Actor.VisualHandler.GetActorVisual();
            if (visual == null)
            {
                Debug.LogWarning($"[InventoryModule] HandleItemSlotted: '{item.GetId()}' -- ActorVisualHandler.GetActorVisual() is null on '{name}' (no ActorVisual set yet?).");
                return;
            }
            item.ItemVisual = visual.EquipItemVisual(item);
            Debug.Log($"[InventoryModule] HandleItemSlotted: '{item.GetId()}' -> ItemVisual = {(item.ItemVisual != null ? item.ItemVisual.name : "null")} on visual '{visual.name}'.");
        }

        private void HandleItemUnslotted(Item item)
        {
            if (item == null) return;
            RemoveItemVisual(item);
            // If still marked equipped, this was a displacement — clean up item state
            if (item.IsEquipped())
                _inventory?.UnequipItem(item);
        }

        // Removes an item's equipped visual from this actor's visual (or despawns a loose visual). Visual-only
        // — it does NOT touch the item's equipped state, so it is safe to run while detaching an inventory.
        private void RemoveItemVisual(Item item)
        {
            if (item == null) return;
            ActorVisual actorVisual = Actor.VisualHandler != null ? Actor.VisualHandler.GetActorVisual() : null;
            if (actorVisual != null)
                actorVisual.UnslotItem(item);
            else if (item.ItemVisual != null)
            {
                item.ItemVisual.Despawn();
                item.ItemVisual = null;
            }
        }

        // ── Public API (delegates to inventory, respects server authority) ─────

        public bool AddItem(ItemData itemData, int amount = 1)
        {
            if (IsServerInitialized)
            {
                return _inventory?.AddItem(itemData, amount) != null;
            }
            ServerAddItem_Rpc(itemData.GetId(), amount);
            return true;
        }

        public bool AddItemById(string itemId, int amount = 1)
        {
            ItemData data = ItemsLibrary.GetItemData(itemId);
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
                ServerEquipItem_Rpc(item.GetInventoryId(), slotType != null ? slotType.GetId() : "");
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
            ItemData data = ItemsLibrary.GetItemData(itemId);
            if (data == null)
            {
                Debug.LogWarning($"[InventoryModule] AddAndEquipItem: no ItemData for id '{itemId}' (check ItemsLibrary.ItemAssets).");
                return false;
            }
            if (IsServerInitialized)
            {
                if (_inventory == null)
                {
                    Debug.LogWarning($"[InventoryModule] AddAndEquipItem: '{itemId}' -- _inventory is null on '{name}'.");
                    return false;
                }
                Item item = _inventory.AddItem(data, amount);
                if (item == null)
                {
                    Debug.LogWarning($"[InventoryModule] AddAndEquipItem: Inventory.AddItem('{itemId}') returned null (no free slot?) on '{name}'.");
                    return false;
                }
                bool equipped = _inventory.EquipItem(item, slotType);
                if (!equipped)
                    Debug.LogWarning($"[InventoryModule] AddAndEquipItem: item '{itemId}' was added but Inventory.EquipItem failed (check EquipableComponent/SuitableSlots and Equipment.SlotTypes) on '{name}'.");
                return true;
            }
            ServerAddAndEquipItem_Rpc(itemId, amount, slotType != null ? slotType.GetId() : "");
            return true;
        }

        public void ClearInventory() => _inventory?.Clear();

        public override void Cleanup()
        {
            base.Cleanup();
            DetachInventory();
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public Item GetItemAtInventoryId(int id) => _inventory?.GetItemAtSlot(id);
        public Item GetItemById(string id) => _inventory?.GetItemById(id);
        public bool ContainsItemReference(Item item) => _inventory?.Contains(item) ?? false;
        public EquipmentSlotType GetEquipmentSlotTypeFromId(string id) => _inventory?.Equipment?.GetEquipmentSlotType(id);

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
            if (!KtNetworkManager.IsClient()) return;
            RefreshEquippedVisuals();
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
            _inventory.EquipItem(item, _inventory.Equipment?.GetEquipmentSlotType(slotId));
        }

        [ServerRpc]
        private void ServerAddAndEquipItem_Rpc(string itemId, int amount, string slotId)
        {
            AddAndEquipItem(itemId, _inventory?.Equipment?.GetEquipmentSlotType(slotId), amount);
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
            _inventory.EquipItem(item, _inventory.Equipment?.GetEquipmentSlotType(slotId));
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
