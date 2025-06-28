using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Database;
using Kuantech.Rpg;
using Kuantech.Utils;

namespace Kuantech.Gridguard
{
    /// <summary>
    /// Reads necessary data from the database and applies it to the actor.
    /// </summary>
    public class StatsLoaderActorBlueprintComponent : ActorBlueprintComponent
    {
        [Serializable]
        public struct AttributeQueryData
        {
            public AttributeAsset AttributeAsset;
            public string BaseValueKey;
            public string ValuePerLevelKey;
        }
        
        public string DatabaseName;
        public string TableName;
        
        public List<AttributeQueryData> StatQueries = new List<AttributeQueryData>();

        private Dictionary<string, AttributeQueryData> _attributeQueryDatas =
            new Dictionary<string, AttributeQueryData>();

        public override void OnActorCreated(Actor actor)
        {
            string actorId = actor.Id;
            KtDatabase db = KtDatabaseManager.GetDatabase(DatabaseName);
            if (db == null) return;
            
            //Set stats
            StatsModule sm = actor.GetModule<StatsModule>();
            if (sm != null)
            {
                foreach (var statQuery in StatQueries)
                {
                    AttributeDefinition attributeDefinition = GetAttributeDefinition(actorId, statQuery);
                    sm.SetAttribute(attributeDefinition);
                }
            }
        }
        
        /// <summary>
        /// Returns attribute definition from attribute asset.
        /// </summary>
        /// <param name="actorId"></param>
        /// <param name="attributeAsset"></param>
        /// <returns></returns>
        public AttributeDefinition GetAttributeDefinition(string actorId, AttributeAsset attributeAsset)
        {
            AttributeDefinition attributeDefinition = new AttributeDefinition()
            {
                AttributeAsset = attributeAsset,
                BaseValue = 0,
                ValuePerLevel = 0,
                ValuePerRank = 0,
            };
            AttributeQueryData attributeQuery = GetAttributeQuery(attributeAsset);
            return GetAttributeDefinition(actorId, attributeQuery);
        }

        public AttributeDefinition GetAttributeDefinition(string actorId, AttributeQueryData attributeQuery)
        {
            AttributeDefinition attributeDefinition = new AttributeDefinition()
            {
                AttributeAsset = attributeQuery.AttributeAsset,
                BaseValue = 0,
                ValuePerLevel = 0,
                ValuePerRank = 0,
            };
            KtDatabase db = KtDatabaseManager.GetDatabase(DatabaseName);
            if (db == null) return attributeDefinition;    
            //Base value
            if (db.GetValue<float>(TableName, actorId, attributeQuery.BaseValueKey, out float baseValue))
            {
                attributeDefinition.BaseValue = baseValue;
            }
                    
            //Value per level
            if (db.GetValue<float>(TableName, actorId, attributeQuery.ValuePerLevelKey, out float valuePerLevel))
            {
                attributeDefinition.ValuePerLevel = valuePerLevel;
            }

            return attributeDefinition;
        }

        /// <summary>
        /// Gets the stat query
        /// </summary>
        /// <param name="attributeAsset"></param>
        /// <returns></returns>
        private AttributeQueryData GetAttributeQuery(AttributeAsset attributeAsset)
        {
            if (_attributeQueryDatas.IsNullOrEmpty())
            {
                foreach(var attributeQuery in StatQueries)
                {
                    _attributeQueryDatas[attributeQuery.AttributeAsset.Id] = attributeQuery;
                }
            }

            if (_attributeQueryDatas.ContainsKey(attributeAsset.Id))
            {
                return _attributeQueryDatas[attributeAsset.Id];
            }

            return new AttributeQueryData()
            {
                AttributeAsset = null,
                BaseValueKey = string.Empty,
                ValuePerLevelKey = string.Empty,
            };
        }
    }
}