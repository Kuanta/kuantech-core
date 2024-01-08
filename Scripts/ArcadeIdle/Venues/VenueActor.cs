using System;
using System.Collections.Generic;
using Kuantech.AI.Utils;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class VenueActor : ArcadeIdleActor, IUnlockable
    {
        [KTTag("VenueTags")]
        public int VenueTag;

        [Header("Navmesh Zones")]
        public List<WorldZone> NavmeshTargetZones;

        [Tooltip("Venue actors that are unlocked at start should tick this")]
        //public bool UnlockedByDefault;
        [NonSerialized] public VenueZone ParentZone;

        public bool UnlockedByDefault;
        [NonSerialized] public bool Unlocked;


        public override void Initialize(ActorState actorState = null)
        {
            if (!UnlockedByDefault) {
                Toggle(false);
                Unlocked = false;
            }
            else{
                Toggle(true);
                Unlocked = true;
            }
            base.Initialize(actorState);
        }

        public override void LoadActorState(ActorState actorState)
        {
            base.LoadActorState(actorState);
            VenueActorState state = (actorState as VenueActorState);
            Unlocked = !state.Locked;
            if (state.Locked && !UnlockedByDefault)
            {
                gameObject.SetActive(false);
            }else{
                gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Gets the destination point that will be used by npcs.
        /// </summary>
        /// <returns></returns>
        public WorldPoint GetDestinationPoint()
        {
            if(NavmeshTargetZones == null || NavmeshTargetZones.Count == 0) 
            {
                return new WorldPoint(){
                    Target = transform,
                    LocalPosition = Vector3.zero,
                    LocalRotation = Quaternion.identity,
                };
            }
            return NavmeshTargetZones.GetRandomElement().SampleWorldPoint();
        }

        #region State
        public bool IsLocked()
        {
            return !Unlocked && !UnlockedByDefault;
        }

        protected override ActorState InstantiateActorState()
        {
            return new VenueActorState(){
                Locked = !Unlocked,
            };
        }

        public override void SetDefaultStateValues()
        {
            base.SetDefaultStateValues();
            Unlocked = UnlockedByDefault;
        }
        public override void DirtyState()
        {
            base.DirtyState();
            if(ParentZone == null || ParentZone.ParentVenue == null)
            {
                Debug.LogError($"{gameObject.name} has null parent venue");
                return;
            }
            ParentZone.ParentVenue.DirtyActorState(this);
        }

        public void Unlock()
        {
            Unlocked = true;
            DirtyState();
        }
        public void Toggle(bool toggle)
        {
            gameObject.SetActive(toggle);
        }
        #endregion
    }
}