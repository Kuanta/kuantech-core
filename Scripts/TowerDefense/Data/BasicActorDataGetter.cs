using System;
using Kuantech.Core;
using Kuantech.Core.Combat;
using Kuantech.Rpg;

namespace Kuantech.TowerDefense
{
    /// <summary>
    /// Implements data getter for spawnables usin stat setter and dps calculator, which need attack pattern setter
    /// </summary>
    [Serializable]
    public class BasicActorDataGetter : SpawnableDataGetter
    {
        public DPSCalculator DpsCalculator;
        
        public override float GetDPS(ActorBlueprint actorBlueprint, int level)
        {
            return DpsCalculator.CalculateDPS(actorBlueprint, level);
        }

        public override float GetAttributeValue(ActorBlueprint actorBlueprint, AttributeAsset attributeAsset, int level)
        {
            StatsSetterComponent statsSetterComponent =
                actorBlueprint.GetActorBlueprintComponent<StatsSetterComponent>();
            AttributeDefinition attributeDefinition = statsSetterComponent.GetAttributeDefinition(attributeAsset);
            if (attributeDefinition == null) return 0;
            return attributeDefinition.GetValue(level, 0);
        }
    }
}