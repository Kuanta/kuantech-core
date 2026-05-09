using System;

namespace Kuantech.Core
{
    [Serializable]
    public abstract class ActorCondition
    {
        public abstract bool IsSatisfied(Actor owner);
    }
}