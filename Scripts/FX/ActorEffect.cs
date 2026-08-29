using System;
using Kuantech.Core;
using Kuantech.Core.FX;

namespace Kuantech.FX
{
    /// <summary>
    /// An effect class that is attached to the actor.
    /// </summary>
    public class ActorFX : Effect
    {
        [NonSerialized] public Actor AttachedActor;

        public void OnAttachedToActor(Actor actor)
        {
            AttachedActor = actor;
            foreach(var fxBehaviour in EffectBehaviours)
            {
                fxBehaviour.OnAttachedToActor(actor);
            }
        }

        public void OnRemovedFromActor()
        {
            foreach (var fxBehaviour in EffectBehaviours)
            {
                fxBehaviour.OnDetachedFromActor();
            }
        }
    }
}