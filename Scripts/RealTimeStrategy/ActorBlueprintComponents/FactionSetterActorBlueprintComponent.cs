using System.Collections.Generic;
using Kuantech.Core;

namespace Kuantech.RealTimeStrategy
{
    public class FactionSetterActorBlueprintComponent : ActorBlueprintComponent
    {
        public int BelongingFaction;
        public List<int> EnemyFactions = new List<int>();
        public List<int> AllyFactions = new List<int>();
        
        public override void OnActorCreated(ActorBlueprint blueprint, Actor actor)
        {
            actor.FactionHandler.BelongingFaction = BelongingFaction;
            actor.FactionHandler.EnemyFactions = EnemyFactions;
            actor.FactionHandler.AlliedFactions = AllyFactions;
        }
    }
}