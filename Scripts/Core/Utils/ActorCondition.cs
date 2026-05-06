namespace Kuantech.Core
{
    public abstract class ActorCondition
    {
        public abstract bool IsSatisfied(Actor owner);
    }
}