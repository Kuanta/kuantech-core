using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using Kuantech.HyperCasual;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

namespace Kuantech.ArcadeIdle
{
    [Serializable]
    public struct UpgradeStatPair
    {
        public UpgradeData UpgradeData;
        [FormerlySerializedAs("Attribute")] public StatAttributeAsset attributeAsset;
    }

    public class ArcadeIdleCharacter : ArcadeIdleActor
    {
        [Header("Character Properties")]
        
        [KTTag("CharacterTag")]
        public int CharacterTag;

        [Header("Interactable")]
        [NonSerialized] public VenueInteractable AssignedInteractable = null;
        [NonSerialized] public InteractionSlot AssignedSlot = null;
        [NonSerialized] public ActorQueue AssignedQueue = null;
        [NonSerialized] public ResourceInventory CharacterInventory;
        [NonSerialized] public float InteractStartTime;
        [NonSerialized] public float LastInteractTime;
        [NonSerialized] public bool StartedInteracting = false;

        [FormerlySerializedAs("MovementSpeedAttribute")]
        [Header("Attributes")]
        [SerializeField] protected StatAttributeAsset movementSpeedAttributeAsset;
        [FormerlySerializedAs("CarryCapacityAttribute")] [SerializeField] protected StatAttributeAsset carryCapacityAttributeAsset;

        [Header("Upgrades")]
        public List<UpgradeStatPair> UpgradeStatPairs;
        private Dictionary<UpgradeData, StatAttributeAsset> _upgradesToAttributes;

        private ArcadeIdleAnimator _animModule;
        private static readonly int InteractHash = Animator.StringToHash("Interacting");
        private static readonly int InteractIndexHash = Animator.StringToHash("InteractionIndex");
        private static readonly int CarryingHash = Animator.StringToHash("Carrying");
        private static readonly int InteractTriggerHash = Animator.StringToHash("Interact");

        protected StatsModule StatsModule;

        public override void Initialize(ActorSerializableData actorSerializableData = null)
        {
            base.Initialize(actorSerializableData);
            CharacterInventory = GetModule<ResourceInventory>();
            _animModule = GetModule<ArcadeIdleAnimator>();
            StatsModule = GetModule<StatsModule>();

            UpgradeManager um = UpgradeManager.GetContext<UpgradeManager>();
            if(um == null) return;
            _upgradesToAttributes = new Dictionary<UpgradeData, StatAttributeAsset>();
            foreach(var pair in UpgradeStatPairs)
            {
                int upgradeRank = UpgradeManager.GetCurrentUpgradeLevel(pair.UpgradeData.UpgradeId);
                StatsModule.SetAttributeRank(pair.attributeAsset.Id,upgradeRank);
                _upgradesToAttributes[pair.UpgradeData] = pair.attributeAsset;
            }
            if(StatsModule != null)
            {
                um.OnUpgrade += OnUpgradeHandler;
                UpdateStats();
            }

            if(CharacterInventory != null)
            {
                CharacterInventory.OnResourceAdded += OnResourceAdded;
                CharacterInventory.OnResourceRemoved += OnResourceRemoved;
            }
        }

        public VenueZone GetCurrentZone()
        {
            if (AssignedInteractable == null) return null;
            return AssignedInteractable.GetParentZone();
        }

        #region Stats
        /// <summary>
        /// Handles the upgrade event
        /// </summary
        /// <param name="sender"></param>
        /// <param name="upgradeData"></param>
        private void OnUpgradeHandler(object sender, UpgradeData upgradeData)
        {
            if (!_upgradesToAttributes.ContainsKey(upgradeData)) return;
            StatsModule.SetAttributeRank(_upgradesToAttributes[upgradeData].Id,
             UpgradeManager.GetCurrentUpgradeLevel(upgradeData.UpgradeId));
            UpdateStats();
        }

        protected virtual void UpdateStats()
        {
            if (StatsModule == null) return;
            foreach (var pair in _upgradesToAttributes)
            {
                StatsModule.SetAttributeRank(pair.Value.Id,
             UpgradeManager.GetCurrentUpgradeLevel(pair.Key.UpgradeId));
            }
            if (CharacterInventory != null && carryCapacityAttributeAsset != null) CharacterInventory.InventoryCapacity = (int)StatsModule.GetAttributeValue(carryCapacityAttributeAsset);
        }
        #endregion

    
        public override void Cleanup()
        {
            base.Cleanup();
            UpgradeManager um = UpgradeManager.GetContext<UpgradeManager>();
            if(um == null) return;
            um.OnUpgrade -= OnUpgradeHandler;
        }

        #region Interactions
        /// <summary>
        /// Called once the character reaches its position in the slot
        /// </summary>
        public virtual void OnReachedToSlot()
        {
            StartInteraction();
            AssignedInteractable.OnActorReachedInteractable(this);
        }
        /// <summary>
        /// Checks whether an actor can interact with an interactable. With overriding this method, desired behaviours can be achieved
        /// like not assigning an npc to a specific shelf if that npc doesn't need that item.
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        public virtual bool CanInteractWith(VenueInteractable interactable)
        {
            return true;
        }
        public bool IsInteracting()
        {
            if (AssignedSlot != null || AssignedQueue != null) return true;
            return false;
        }
        public void StartInteraction()
        {
            InteractStartTime = Time.time;
            StartedInteracting = true;

            //Start animation if index is a valid one
            if (_animModule != null && AssignedSlot.InteractionAnimationIndex >= 0 && AssignedSlot.LoopingAnimation)
            {
                _animModule.Animator.SetBool(InteractHash, true);
                _animModule.Animator.SetInteger(InteractIndexHash, AssignedSlot.InteractionAnimationIndex);
            }
        }

        public void OnHandleActor()
        {
            LastInteractTime = Time.time;
            //Start animation if index is a valid one
            if (_animModule != null && AssignedSlot.InteractionAnimationIndex >= 0 && !AssignedSlot.LoopingAnimation)
            {
                _animModule.Animator.SetTrigger(InteractTriggerHash);
                _animModule.Animator.SetInteger(InteractIndexHash, AssignedSlot.InteractionAnimationIndex);
            }
        }
        public float GetInteractionElapsedTime()
        {
            if (!IsInteracting() || !StartedInteracting) return 0;
            return Time.time - InteractStartTime;
        }
        public void SetInteractable(VenueInteractable venueInteractable)
        {
            AssignedInteractable = venueInteractable;
        }

        public virtual void OnAssignedToSlot(InteractionSlot slot)
        {
            AssignedInteractable = slot.ParentInteractable;
            InteractStartTime = Time.time;
            StartedInteracting = false;
        }

        private IEnumerator _endInteractionCoroutine = null;
        [Button("EndInteractions")]
        /// <summary>
        /// Ends the interaction with the currenct interactable
        /// </summary>
        /// <param name="immediateEnd">If set to true, ending won't be delayed even if the slot has end delay</param>
        public void EndInteraction(bool immediateEnd = false)
        {
            if(_endInteractionCoroutine != null) return;

            if(immediateEnd || (AssignedSlot != null && AssignedSlot.InteractionEndDelay <= 0f))
            {
                //If left to coroutines, immediate end doesn't work
                _EndInteraction();
                return;
            }
            _endInteractionCoroutine = EndInteractionRoutine(AssignedSlot != null && !immediateEnd ? AssignedSlot.InteractionEndDelay : 0);
            StartCoroutine(_endInteractionCoroutine);
        }

        private IEnumerator EndInteractionRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            _EndInteraction();
        }
        
        /// <summary>
        /// Removes the actor from its attached interactable
        /// </summary>
        private void _EndInteraction()
        {
            if (AssignedInteractable != null) AssignedInteractable.RemoveActor(this);

            //Stop interaction animation
            if (_animModule != null) _animModule.Animator.SetBool(InteractHash, false);
            _endInteractionCoroutine = null;
        }
        #endregion

        #region Inventory

        public void OnResourceAdded((ResourceData, int) args)
        {
            if (_animModule == null || CharacterInventory.ResourceDisplayer == null || args.Item1.IsCurrency()) return;
            if(CharacterInventory.ResourceDisplayer.AcceptsResource(args.Item1))
            {
                _animModule.Animator.SetBool(CarryingHash, true);
            }
        }
        public void OnResourceRemoved((ResourceData, int) args)
        {
            if (_animModule == null || CharacterInventory.ResourceDisplayer == null || args.Item1.IsCurrency()) return;
            int displayedResourceCount = CharacterInventory.ResourceDisplayer.GetDisplayedResourceCount();
            _animModule.Animator.SetBool(CarryingHash, displayedResourceCount > 0);
        }
        #endregion

        public CharacterState GetCharacterState()
        {
            return new CharacterState()
            {
                WorkerTag = CharacterTag,
                actorSerializableData = GetActorState(),
                PosX = transform.position.x,
                PosZ = transform.position.z,
                RotY = transform.rotation.eulerAngles.y,
            };
        }
    }
}