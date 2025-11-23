using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Database;
using Kuantech.Rpg;
using Kuantech.Utils;
using UnityEngine;

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
                
        //DEPRECATED, use UpdateFromDatabaseRow instead
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

        public override void UpdateFromDatabaseRow(DataTable.RowData rowData)
        {
            foreach (var attribDefinition in AttributeDefinitions)
            {
                string attributeId = attribDefinition.AttributeAsset.Id;
                try
                {
                    attribDefinition.BaseValue = 0;
                    attribDefinition.ValuePerLevel = 0;
                    attribDefinition.ValuePerRank = 0;
                    string attributeStringVal = rowData.GetValue<string>(attributeId);
                    if (attributeStringVal.IsNullOrEmpty() || attributeStringVal == "") continue;
                    string[] parts = attributeStringVal.Split('/');
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
                catch (Exception e)
                {
                    Debug.LogWarning(e.Message);
                }
              
            }
        }

    }

}