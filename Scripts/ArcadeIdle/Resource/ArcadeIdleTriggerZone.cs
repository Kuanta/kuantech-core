using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ArcadeIdleTriggerZone : MonoBehaviour
    {
        [NonSerialized] public ArcadeIdleCharacter CurrentActor;
        public HashSet<ArcadeIdleCharacter> EnteredActors = new HashSet<ArcadeIdleCharacter>();

        private void OnTriggerEnter(Collider other)
        {
            //todo: Implement a filtering system here
            if (other.TryGetComponent(out ArcadeIdleCharacter character))
            {
                EnteredActors.Add(character);
                CalculateCurrentActor();
                OnActorEnter(character);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out ArcadeIdleCharacter character))
            {
                EnteredActors.Remove(character);
                CalculateCurrentActor();
                OnActorLeave(character);
            }
        }

        private void CalculateCurrentActor()
        {
            if (EnteredActors.Count == 0)
            {
                CurrentActor = null;
            }
            
            foreach (var actor in EnteredActors)
            {
                CurrentActor = actor;
                OnCurrentActorChange();
                return;
            }
        }

        protected virtual void OnCurrentActorChange()
        {
            
        }

        public ArcadeIdleActor GetCurrentActor()
        {
            return CurrentActor;
        }

        protected virtual void OnActorEnter(ArcadeIdleCharacter character)
        {

        }

        protected virtual void OnActorLeave(ArcadeIdleCharacter character)
        {

        }

    }
}