using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    /// <summary>
    /// Trigger zone for arcade idle actors
    /// </summary>
    public class ArcadeIdleActorTriggerZone : MonoBehaviour
    {
        public HashSet<ArcadeIdleActor> EnteredActors = new HashSet<ArcadeIdleActor>();

        private void OnTriggerEnter(Collider other)
        {
            //todo: Implement a filtering system here
            if (other.TryGetComponent(out ArcadeIdleActor actor))
            {
                EnteredActors.Add(actor);
                OnActorEnter(actor);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out ArcadeIdleActor actor))
            {
                EnteredActors.Remove(actor);
                OnActorLeave(actor);
            }
        }

        protected virtual void OnActorEnter(ArcadeIdleActor actor)
        {

        }

        protected virtual void OnActorLeave(ArcadeIdleActor actor)
        {

        }

    }
}