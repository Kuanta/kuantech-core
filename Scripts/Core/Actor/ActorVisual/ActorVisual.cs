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

        [Header("Visual Parts")] 
        public ActorVisualPartsHandler VisualPartsHandler;


        [Header("Shader Effects")] 
        public List<ShaderEffect> ShaderEffects;
        
        //Actor visual Modules
        public ModuleHandler<ActorVisualModule> ModuleHandler;
        
        //Runtime
        [NonSerialized] public bool Initialized;
        [NonSerialized] public Actor ParentActor;

        #region Lifecycle

        /// <summary>
        /// Initializes the actor visual
        /// </summary>
        public void Initialize()
        {
            if (Initialized) return;
            Initialized = true;

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
        /// <summary>
        /// Checks whether actor visual has the slot
        /// </summary>
        /// <param name="slotType"></param>
        /// <returns></returns>
        public bool HasSlotFor(EquipmentSlotType slotType)
        {
            ActorSlotsHandler slotsHandler = ParentActor.GetModule<ActorSlotsHandler>();
            if (slotsHandler == null) return false;
            var slot = slotsHandler.GetSlot(slotType.SlotName);
            return slot != null;
        }

        
        /// <summary>
        /// Gets the object slot for equipment type
        /// </summary>
        /// <param name="equipmentSlotType"></param>
        /// <returns></returns>
        public Transform GetObjectSlot(string slotName)
        {
            // 1. Öncelik: Actor modülü üzerinden slot bulmak (Gameplay)
            if (ParentActor != null)
            {
                var slotsHandler = ParentActor.GetModule<ActorSlotsHandler>();
                if (slotsHandler != null)
                {
                    return slotsHandler.GetSlot(slotName);
                }
            }

            // 2. Fallback: Actor yoksa (Manken modu), kendi çocuklarında ara (Menu)
            // Recursive find yapabiliriz veya VisualPartsHandler içinden bakabiliriz.
            // En basiti recursive aramadır:
            return transform.FindDeepChild(slotName); 
        }
        
        /// <summary>
        /// Slots a visual to the given slot
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="objectToSlot"></param>
        /// <returns></returns>
        public ItemVisual SlotItem(EquipmentSlotType slot, Item itemToSlot)
        {
            if (!HasSlotFor(slot)) return null;
            
            //Clear occupying slots
            foreach (var occupying in itemToSlot.GetOccupyingSlots(slot))
            {
                ToggleVisualPartForSlot(occupying, false);
            }

            Transform socket = GetObjectSlot(slot.SlotName);

            //is in place?
            ItemVisual visual = itemToSlot.SpawnItemVisual();
            if (visual == null) return null;
            visual.gameObject.AttachToParent(socket);
            return visual;
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