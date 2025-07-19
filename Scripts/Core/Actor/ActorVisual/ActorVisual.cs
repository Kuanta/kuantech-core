using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.FX;
using Kuantech.Rpg.Inventory;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public class ItemSocket
    {
        public EquipmentSlotType SlotType;
        public Transform Socket;
        public ItemVisual CurrentObject;
    }
    
    [Serializable]
    public struct InPlaceItemEntry
    {
        public ItemVisual Visual;
        public ItemDataAsset ItemData;
    }

    [Serializable]
    public struct ActorVisualSerializableData
    {
        public List<ActorVisualPartSerializableData> VisualPartsData;
    }

    [Serializable]
    public struct ActorVisualPartSerializableData
    {
        public string SlotName;
        public int SlottedPartIndex;
    }
    
    public class ActorVisual : MonoBehaviour
    {
        [Header("Animations")] 
        public Animator Animator;

        [Header("Bones")] 
        public List<ItemSocket> ObjectSlots;

        [Header("Visual Parts")] 
        public ActorVisualPartsHandler VisualPartsHandler;
        
        [Header("Inplace Items")]
        public List<InPlaceItemEntry> InPlaceItemsList;

        [Header("Shader Effects")] 
        public List<ShaderEffect> ShaderEffects;
        
        //Actor visual Modules
        public ModuleHandler<ActorVisualModule> ModuleHandler;
        
        //Runtime
        [NonSerialized] public bool Initialized;
        [NonSerialized] public Actor ParentActor;
        private Dictionary<string, InPlaceItemEntry> _inPlaceItemsMap;

        //Equipment Slot Types To Bone Slots
        private Dictionary<EquipmentSlotType, ItemSocket> _slotsToBoneSlots;

        #region Lifecycle

        /// <summary>
        /// Initializes the actor visual
        /// </summary>
        public void Initialize()
        {
            if (Initialized) return;
            Initialized = true;
            _inPlaceItemsMap = new Dictionary<string, InPlaceItemEntry>();
            foreach (var inplace in InPlaceItemsList)
            {
                _inPlaceItemsMap[inplace.ItemData.GetItemId()] = inplace;
                inplace.Visual.IsInPlace = true;
            }
            VisualPartsHandler.Initialize();

            ModuleHandler = new ModuleHandler<ActorVisualModule>();
            ActorVisualModule[] actorVisualModules = GetComponentsInChildren<ActorVisualModule>();
            foreach (var module in actorVisualModules)
            {
                ModuleHandler.AddModule(module);
                module.Initialize(this);
            }
        }

        public void OnAttachedToActor(Actor actor)
        {
            Initialize(); //Initialize if not
            foreach (var module in ModuleHandler.GetAllModules())
            {
                module.OnActorVisualSet(actor);
            }
        }

        public void OnRemovedFromActor(Actor actor)
        {
            
        }
        #endregion

        #region Actor Visual Modules

        public T GetModule<T>() where T : ActorVisualModule
        {
            return ModuleHandler.GetModule<T>() as T;
        }

        #endregion

        #region Item Slotting
        public bool HasSlotFor(EquipmentSlotType slotType)
        {
            if (_slotsToBoneSlots == null) return false;
            return _slotsToBoneSlots.ContainsKey(slotType);
        }
        
        /// <summary>
        /// Checks whether slot is available for given equipment slot type
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public bool IsSlotAvailable(EquipmentSlotType slot)
        {
            if (!HasSlotFor(slot)) return false;
            return GetObjectSlot(slot)?.CurrentObject;
        }
        
        /// <summary>
        /// Gets the object slot for equipment type
        /// </summary>
        /// <param name="equipmentSlotType"></param>
        /// <returns></returns>
        public ItemSocket GetObjectSlot(EquipmentSlotType equipmentSlotType)
        {
            if (!HasSlotFor(equipmentSlotType)) return null;
            return _slotsToBoneSlots[equipmentSlotType];
        }
        
        /// <summary>
        /// Slots a visual to the given slot
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="objectToSlot"></param>
        /// <returns></returns>
        public bool SlotItem(EquipmentSlotType slot, Item itemToSlot)
        {
            if (!HasSlotFor(slot)) return false;
            
            //Clear occupying slots
            foreach (var occupying in itemToSlot.GetOccupyingSlots(slot))
            {
                ClearSlot(occupying);
                ToggleVisualPartForSlot(occupying, false);
            }

            ItemSocket socket = GetObjectSlot(slot);

            //is in place?
            if (_inPlaceItemsMap.ContainsKey(itemToSlot.Data.Id))
            {
                InPlaceItemEntry entry = _inPlaceItemsMap[itemToSlot.Data.Id];
                socket.CurrentObject = entry.Visual;
                ToggleInplaceItem(itemToSlot.GetId(), true);
            }
            else
            {
                ItemVisual visual = itemToSlot.SpawnItemVisual();
                visual.gameObject.AttachToParent(socket.Socket);
                socket.CurrentObject = visual;
            }

            return true;
        }
        
        /// <summary>
        /// Unslots an item
        /// </summary>
        /// <param name="itemToUnslot"></param>
        public void UnslotItem(Item itemToUnslot)
        {
            EquipmentSlotType slotType = itemToUnslot.CurrentSlot;
            foreach (var occupying in itemToUnslot.GetOccupyingSlots(slotType))
            {
                ClearSlot(occupying);
                ToggleVisualPartForSlot(occupying, false);
            }
        }
        /// <summary>
        /// Toggles in place item
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="toggle"></param>
        private void ToggleInplaceItem(string itemId, bool toggle)
        {
            _inPlaceItemsMap[itemId].Visual.gameObject.SetActive(toggle);
        }
        
        /// <summary>
        /// Toggles the visual part
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="toggle"></param>
        private void ToggleVisualPartForSlot(EquipmentSlotType slot, bool toggle)
        {
            VisualPartsHandler.ToggleVisualPart(slot, toggle);
        }

      
        /// <summary>
        /// Clears an item socket
        /// </summary>
        /// <param name="slotType"></param>
        public void ClearSlot(EquipmentSlotType slotType)
        {
            ItemSocket slot = GetObjectSlot(slotType);
            if (slot == null || slot.CurrentObject == null) return;
            slot.CurrentObject.Despawn();
            
            //Toggles the default visual part
            ToggleVisualPartForSlot(slotType, true);
        }
        #endregion

        #region Shader Effects

        public void AddShaderEffect(ShaderEffect effect)
        {
            if (effect == null) return;
            ShaderEffects.Add(effect);
            effect.DetectAllRenderers(gameObject);
        }

        public void DetectShaderEffects()
        {
            ShaderEffects = GetComponentsInChildren<ShaderEffect>().ToList();
            foreach (var shaderEffect in ShaderEffects)
            {
                shaderEffect.DetectAllRenderers(gameObject);
            }
        }
        #endregion

        #region Events

        public void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            foreach (var module in ModuleHandler.GetAllModules())
            {
                module.OnActorStateChanged(oldState, newState);
            }
        }
        #endregion
    }
}