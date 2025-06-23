using System;

namespace Kuantech.Core
{
    [Serializable]
    public abstract class ActorBlueprintComponent
    {
        public abstract void OnActorCreated(Actor actor);
    }
}