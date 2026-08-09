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

        // Was a Unity Update message. Every ActorModule that keeps one is ticked by the engine for
        // every instance every frame, which is both the per-component overhead ActorManager exists to
        // remove and a way out of the update rate the actor asked for.
        public override void ModuleUpdate(float deltaTime)
        {
            base.ModuleUpdate(deltaTime);   
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