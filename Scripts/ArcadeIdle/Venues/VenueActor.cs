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
        public List<AIZone> NavmeshTargetZones;

        [Tooltip("Venue actors that are unlocked at start should tick this")]
        //public bool UnlockedByDefault;
        [NonSerialized] public VenueZone ParentZone;

        public bool UnlockedByDefault;
        [NonSerialized] public bool Unlocked;


        public override void Initialize(ActorSerializableData actorSerializableData = null)
        {
            if (!UnlockedByDefault) {
                Toggle(false);
                Unlocked = false;
            }
            else{
                Toggle(true);
                Unlocked = true;
            }
            base.Initialize(actorSerializableData);
        }

        public override void LoadActorState(ActorSerializableData actorSerializableData)
        {
            base.LoadActorState(actorSerializableData);
            VenueActorSerializableData serializableData = (actorSerializableData as VenueActorSerializableData);
            Unlocked = !serializableData.Locked;
            if (serializableData.Locked && !UnlockedByDefault)
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

        protected override ActorSerializableData InstantiateActorState()
        {
            return new VenueActorSerializableData(){
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
            //ParentZone.ParentVenue.DirtyActorState(this);
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