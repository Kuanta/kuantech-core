using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    /// <summary>
    /// Actor component to interact with resource source node
    /// </summary>
                
    public class ResourceNodePicker : ActorModule {
        [SerializeField] private ArcadeIdleActorTriggerZone ActorTriggerZone;
        [SerializeField] private float InteractionPeriod;
        
        private float _lastInteractedTime;

        private void Update()
        {   
            if(!Initialized) return;

            if(ActorTriggerZone.EnteredActors.IsNullOrEmpty()) return;
            HandleEnteredActors();
        }

        private void HandleEnteredActors()
        {
            if(Time.time - _lastInteractedTime < InteractionPeriod) return;
            bool interacted = false;
            foreach(var actor in ActorTriggerZone.EnteredActors)
            {
                ResourceSourceNode node = actor.GetModule<ResourceSourceNode>();
                if(node == null) continue;
                interacted = true;
                node.Interact(this);
            }
            if(interacted) _lastInteractedTime = Time.time;
        }
    }

}