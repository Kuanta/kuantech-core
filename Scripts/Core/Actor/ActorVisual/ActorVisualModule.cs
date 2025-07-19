using System;
using UnityEngine;

namespace Kuantech.Core
{
    public class ActorVisualModule : MonoBehaviour
    {
        //Runtime
        [NonSerialized] public ActorVisual ActorVisual;
        [NonSerialized] public Actor Actor;
        
        public virtual void Initialize(ActorVisual actorVisual)
        {
            ActorVisual = actorVisual;
        }
        
        public void OnActorVisualSet(Actor actor)
        {
            Actor = actor;
        }

        public virtual void OnActorStateChanged(ActorState  oldState, ActorState newState)
        {
            
        }

        public virtual void Reset()
        {
            
        }
    }
}