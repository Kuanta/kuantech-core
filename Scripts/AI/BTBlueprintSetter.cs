using Kuantech.Core;

namespace Kuantech.AI
{
    public class BTBlueprintSetter : ActorBlueprintComponent
    {
        public BehaviourTreeBlueprint BTBlueprint;
        public override void OnActorCreated(ActorBlueprint blueprint, Actor actor)
        {
            BTAgent btAgent = actor.GetModule<BTAgent>();
            if (btAgent == null) return;
            btAgent.SetBehaviourTree(BTBlueprint.CreateBehaviourTree());
        }
    }
}