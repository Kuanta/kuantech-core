using System;
using Kuantech.Core;
using Kuantech.Rpg;

namespace Kuantech.TowerDefense
{
    [Serializable]
    public abstract class SpawnableDataGetter
    {
        public abstract float GetDPS(ActorBlueprint actorBlueprint, int level);

        public abstract float GetAttributeValue(ActorBlueprint actorBlueprint, AttributeAsset attributeAsset,
            int level);
    }
}