using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Database;
using Kuantech.Rpg;
using Kuantech.Utils;

namespace Kuantech.Core
{
    public class StatsSetterComponent : ActorBlueprintComponent
    {
        public List<AttributeDefinition> AttributeDefinitions;
        public override void OnActorCreated(ActorBlueprint blueprint, Actor actor)
        {
            StatsModule sm = actor.GetModule<StatsModule>();
            foreach (var def in AttributeDefinitions)
            {
                sm.SetAttribute(def, true);
            }
        }

        public AttributeDefinition GetAttributeDefinition(AttributeAsset attributeAsset)
        {
            foreach (var def in AttributeDefinitions)
            {
                if (def.AttributeAsset == attributeAsset) return def;
            }
            return null;
        }
            
        public void UpdateVariablesFromDatabase(KtDatabase database, string tableName, string rowId)
        {
            foreach (var attribDefinition in AttributeDefinitions)
            {
                string attributeId = attribDefinition.AttributeAsset.Id;
                string entry = database.GetString(tableName, rowId, attributeId);
                
                //base and per level must be sepeated by '/'
                string[] parts= entry.Split('/');
                if(parts == null || parts.Length <= 0) continue;
                
                //Base Value
                attribDefinition.BaseValue = parts[0].TryParseFloat(attribDefinition.BaseValue);

                if (parts.Length > 1)
                {
                    attribDefinition.ValuePerLevel = parts[1].TryParseFloat(attribDefinition.ValuePerLevel);
                }

                if (parts.Length > 2)
                {
                    attribDefinition.ValuePerRank = parts[2].TryParseFloat(attribDefinition.ValuePerRank);
                }
            }
        }
    }

}