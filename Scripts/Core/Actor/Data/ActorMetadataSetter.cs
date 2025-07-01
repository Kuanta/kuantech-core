namespace Kuantech.Core
{
    public class ActorMetadataSetter : ActorBlueprintComponent
    {
        public int FactionId;
        public override void OnActorCreated(Actor actor)
        {
            actor.FactionId = FactionId;
        }
    }
}