namespace Kuantech.Core
{
    public class ActorMetadataSetter : ActorBlueprintComponent
    {
        public int FactionId;
        public override void OnActorCreated(ActorBlueprint blueprint, Actor actor)
        {
            actor.FactionId = FactionId;
        }
    }
}