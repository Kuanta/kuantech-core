using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.FX;
using Kuantech.Inventory;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class ActorVisual : MonoBehaviour
    {
        [Header("Info")]
        public string ActorVisualId;
        
        [Header("Animations")] 
        public Animator Animator;

        [Header("Visual Parts")]
        public ActorVisualPartsHandler VisualPartsHandler;

        [Header("Skinned Mesh")]
        public Transform SkeletonRoot;


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

            VisualPartsHandler.Initialize(this);

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
        /// Slots a visual to the given slot. Hides default visual parts declared on the visual.
        /// </summary>
        public ItemVisual SlotItem(EquipmentSlotType slot, Item itemToSlot)
        {
            // In-place visuals live on the model — no socket needed, check before HasSlotFor
            string templateId = itemToSlot.Data.ItemTemplateId;
            ItemVisual visual = FindInPlaceVisual(templateId);
            if (visual != null)
            {
                visual.IsInPlace = true;
                visual.gameObject.SetActive(true);
                visual.Spawn(itemToSlot.Data);
                visual.RuntimeVisualSlot = visual.VisualSlot;
                VisualPartsHandler.OnSlotEquipped(visual.VisualSlot, visual);
                return visual;
            }

            // Spawned visuals require a socket
            if (!HasSlotFor(slot)) return null;

            visual = itemToSlot.SpawnItemVisual();
            if (visual == null) return null;

            if (visual.IsSkinned)
            {
                visual.gameObject.AttachToParent(transform);
                if (SkeletonRoot != null)
                    visual.RebindBones(SkeletonRoot.GetComponentsInChildren<Transform>());
            }
            else
            {
                Transform socket = GetObjectSlot(slot.SlotName);
                if (socket == null) { visual.Despawn(); return null; }
                visual.gameObject.AttachToParent(socket);
            }

            visual.RuntimeVisualSlot = visual.VisualSlot;
            VisualPartsHandler.OnSlotEquipped(visual.VisualSlot, visual);
            return visual;
        }

        /// <summary>
        /// Despawns the item's visual and restores any hidden default parts.
        /// </summary>
        public void UnslotItem(Item item)
        {
            if (item == null || item.ItemVisual == null) return;
            VisualPartsHandler.OnSlotUnequipped(item.ItemVisual.RuntimeVisualSlot, item.ItemVisual);
            item.ItemVisual.Despawn();
            item.ItemVisual = null;
        }

        private ItemVisual FindInPlaceVisual(string templateId)
        {
            if (string.IsNullOrEmpty(templateId)) return null;
            var visuals = GetComponentsInChildren<ItemVisual>(includeInactive: true);
            foreach (var v in visuals)
                if (v.TemplateId == templateId && !v.gameObject.activeSelf)
                    return v;
            return null;
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