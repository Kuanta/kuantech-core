using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
#if NETWORKING_FISHNET
using FishNet.Connection;
using FishNet.Object;
#endif
using Kuantech.Rpg.Managers;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Rpg
{
    [Serializable]
    public class StatDictionary : SerializableDictionary<AttributeAsset, Attribute> { }

    [Serializable]
    public class ModifierDataDictionary : SerializableDictionary<AttributeAsset, StatModifierData> { }

    [Serializable]
    public class StatsSerializableData : ActorModuleSerializableData
    {
        public int Level;
        public List<SerializableAttributeDefinition> Attributes; // BaseValue, ValuePerRank, ValuePerLevel
        public Dictionary<string, int> AttributeRanks;
        public Dictionary<string, float> ResourceValues;
    }

    /// <summary>
    /// Inspector-friendly spawn-time stat setup. Uses string IDs instead of asset references
    /// so it can be filled directly in ActorDataManager without ScriptableObject references.
    /// Add this to ActorSpawnData.ModuleDatas with ModuleId matching the StatsModule's ModuleId.
    /// </summary>
    [Serializable]
    public class StatsModuleSpawnData : ActorModuleSerializableData
    {
        public int Level;
        public List<SerializableAttributeDefinition> Attributes;
    }
    
    public class StatsModule : ActorModule
    {
        [Header("Stats")]
        public List<AttributeDefinition> Stats;
        private Dictionary<string, Attribute> _statMap;
        public static float LevelFormulaX = 0.4f;

        [Header("Resources")] 
        public List<ResourceDefinition> ResourceDefinitions;
        public ResourceManager ResourceManager;
        
        //Level
        public LevelVariable ActorLevel;

        //Events
        public UnityAction<int> LevelChangeEvent;
        public UnityAction ExperienceEarnedEvent;

        [NonSerialized] private Dictionary<AttributeAsset, HashSet<StatModifier>> Modifiers;
        [NonSerialized] private Queue<AttributeAsset> DirtiedStats = new Queue<AttributeAsset>();

        public override void Initialize()
        {
            base.Initialize();
            _statMap = new Dictionary<string, Attribute>();
            ApplyStatsTable(Stats);
            ResourceManager = new ResourceManager();
            ResourceManager.Initialize(this);
        }

        public override void ResetModule()
        {
            base.ResetModule();
            UpdateStatModifiers();
            ResourceManager.Refresh();
        }

        public override void Cleanup()
        {
            base.Cleanup();
            ClearModifiers();
        }
        
        public void ApplyStatsTable(List<AttributeDefinition> defaultAttributes)
        {
            if (defaultAttributes.IsNullOrEmpty()) return;
            foreach(var attributeDefinition in defaultAttributes)
            {
                CreateAttribute(attributeDefinition);
            }
        }

        public void CreateAttribute(AttributeDefinition attributeDefinition)
        {
            Attribute attribute = new Attribute();
            attribute.ApplyAttributeDefinition(attributeDefinition);
            _statMap[attribute.attributeAsset.Id] = attribute;
        }
        
        public override void LoadState(ActorModuleSerializableData serializableData)
        {
            base.LoadState(serializableData);
            if (serializableData is StatsModuleSpawnData spawnData)
                ApplyFromSpawnData(spawnData);
            else if (serializableData is StatsSerializableData statsData)
                SetStatStates(statsData);
        }

        private void ApplyFromSpawnData(StatsModuleSpawnData spawnData)
        {
            ExecuteSetLevel(spawnData.Level);

            if (spawnData.Attributes == null) return;
            foreach (var def in spawnData.Attributes)
            {
                AttributeAsset asset = RpgManager.GetAttributeAssetById(def.AttributeId);
                if (asset == null)
                {
                    Debug.LogWarning($"[StatsModule] ApplyFromSpawnData: attribute '{def.AttributeId}' not found in RpgManager.");
                    continue;
                }
                ExecuteSetAttribute(new AttributeDefinition
                {
                    AttributeAsset  = asset,
                    BaseValue       = def.BaseValue,
                    ValuePerRank    = def.ValuePerRak,
                    ValuePerLevel   = def.ValuePerLevel,
                }, insertAttribute: true);
            }
        }

        public override void ModuleUpdate()
        {
            base.ModuleUpdate();
            ResourceManager.TickResources(Time.deltaTime);
        }
        
        #region Level & Experience

        public LevelVariable GetActorLevelVariable()
        {
            return ActorLevel;
        }
        
        /// <summary>
        /// Returns the current actor level
        /// </summary>
        /// <returns></returns>
        public int GetActorLevel()
        {
            return ActorLevel.CurrentLevel;
        }
        
        /// <summary>
        /// Adds experience points to the actor. The actor is leveled up if enough experience is earned
        /// </summary>
        /// <param name="experience"></param>
        public void AddExperience(int experience)
        {
            if (!IsServerInitialized) return;
            ExecuteAddExperience(experience);
            if (IsSpawned) ObserverSetActorLevel_Rpc(ActorLevel.CurrentLevel, experience);
        }

        private void ExecuteAddExperience(int experience)
        {
            int currentLevel = GetActorLevel();
            ActorLevel.AddValue(experience);
            int newLevel = ActorLevel.CurrentLevel;
            if (newLevel > currentLevel) ExecuteSetLevel(newLevel);
            ExperienceEarnedEvent?.Invoke();
        }

        /// <summary>
        /// Sets the level of the player
        /// </summary>
        /// <param name="level"></param>
        public void SetLevel(int level)
        {
            if (!IsServerInitialized) return;
            ExecuteSetLevel(level);
            if (IsSpawned) ObserverSetActorLevel_Rpc(level, 0);
        }

        private void ExecuteSetLevel(int level)
        {
            ActorLevel.SetLevel(level);
            if (ResourceManager != null) ResourceManager.Refresh();
            LevelChangeEvent?.Invoke(level);
        }

        public float GetExperienceRequiredToLevelUp()
        {
            return ActorLevel.GetRequiredFromCurrentToNextLevel();
        }
        #endregion
        
        #region Attributes

        public Attribute GetAttribute(AttributeAsset attributeAsset)
        {
            if (attributeAsset == null) return null;
            return GetAttribute(attributeAsset.Id);
        }
        
        public Attribute GetAttribute(string attributeId)
        {
            if (!_statMap.ContainsKey(attributeId)) return null;
            return _statMap[attributeId];
        }
        
        /// <summary>
        /// Returns the max value for an attribute
        /// </summary>
        /// <param name="attributeAsset"></param>
        /// <returns></returns>
        public float GetAttributeMaxValue(AttributeAsset attributeAsset)
        {
            Attribute att = GetAttribute(attributeAsset);
            if (att == null) return 0f;
            return att.Limits.y;
        }
        
        /// <summary>
        /// Returns the minimum value for an attribute
        /// </summary>
        /// <param name="attributeAsset"></param>
        /// <returns></returns>
        public float GetAttributeMinValue(AttributeAsset attributeAsset)
        {
            Attribute att = GetAttribute(attributeAsset);
            if (att == null) return 0f;
            return att.Limits.x;
        }
        
        public float GetAttributeValue(AttributeAsset attributeAsset)
        {
            if(attributeAsset == null)
            {
                return 0f;
            }
            return GetAttributeValue(attributeAsset.Id);
        }

        public float GetAttributeValue(string statId)
        {
            Attribute att = GetAttribute(statId);
            if (att == null) return 0f;
            return att.GetValue(GetActorLevel());
        }

        /// <summary>
        /// Sets the attribute
        /// </summary>
        /// <param name="attributeDefinition"></param>
        /// <param name="insertAttribute"></param>
        public void SetAttribute(AttributeDefinition attributeDefinition, bool insertAttribute=false)
        {
            if (!IsServerInitialized) return;
            ExecuteSetAttribute(attributeDefinition, insertAttribute);
            if (IsSpawned) TargetSetStat_Rpc(Owner, attributeDefinition, insertAttribute);
        }

        private void ExecuteSetAttribute(AttributeDefinition attributeDefinition, bool insertAttribute = false)
        {
            if (attributeDefinition.AttributeAsset == null) return;
            Attribute att = GetAttribute(attributeDefinition.AttributeAsset);
            if (att == null)
            {
                if (insertAttribute)
                {
                    CreateAttribute(attributeDefinition);
                    att = GetAttribute(attributeDefinition.AttributeAsset);
                }
                else
                {
                    return;
                }
            }
            att.ApplyAttributeDefinition(attributeDefinition);
        }
        
        /// <summary>
        /// Sets the base value of an attribute
        /// </summary>
        /// <param name="attributeAsset"></param>
        /// <param name="value"></param>
        public void SetAttributeValue(AttributeAsset attributeAsset, float value)
        {
            if (!IsServerInitialized) return;
            ExecuteSetAttributeValue(attributeAsset, value);
            if (IsSpawned) TargetSetAttributeValue_Rpc(Owner, attributeAsset.Id, value);
        }

        private void ExecuteSetAttributeValue(AttributeAsset attributeAsset, float value)
        {
            Attribute att = GetAttribute(attributeAsset);
            if (att == null) return;
            att.BaseValue = value;
        }

        private void ExecuteSetAttributeValue(string attributeId, float value)
        {
            Attribute att = GetAttribute(attributeId);
            if (att == null) return;
            att.BaseValue = value;
        }

        /// <summary>
        /// Sets the rank of the attribute
        /// </summary>
        /// <param name="statId"></param>
        /// <param name="rank"></param>
        public void SetAttributeRank(string statId, int rank)
        {
            if(!_statMap.ContainsKey(statId))
            {
                return;
            } 
            _statMap[statId].Rank = rank;
        }

        /// <summary>
        /// Returns the current rank of the attribute.
        /// </summary>
        /// <param name="attributeAsset">Attribute object</param>
        /// <returns></returns>
        public int GetAttributeRank(AttributeAsset attributeAsset)
        {
            return GetAttributeRank(attributeAsset.Id);
        }

        /// <summary>
        /// Returns the current rank of the attribute by attribute Id
        /// </summary>
        /// <param name="attributeId">Id of the attribute</param>
        /// <returns></returns>
        public int GetAttributeRank(string attributeId)
        {
            if (!_statMap.ContainsKey(attributeId)) return 0;
            return _statMap[attributeId].Rank;
        }

        /// <summary>
        /// Increases the rank of given attribute.
        /// </summary>
        /// <param name="attributeAsset">Attribute object</param>
        /// <param name="amountToIncrease">Amount to increase</param>
        public void IncreaseAttributeRank(AttributeAsset attributeAsset, int amountToIncrease)
        {
            IncreaseAttributeRank(attributeAsset.Id, amountToIncrease);
        }

        /// <summary>
        /// Increases the rank of given attribute.
        /// </summary>
        /// <param name="attributeId">Attribute id</param>
        /// <param name="amountToIncrease">Amount to increase</param>
        public void IncreaseAttributeRank(string attributeId, int amountToIncrease)
        {
            int currentRank = GetAttributeRank(attributeId);
            SetAttributeRank(attributeId, currentRank + amountToIncrease);
        }
        #endregion

        #region Resources
        
        /// <summary>
        /// Gets resource data
        /// </summary>
        /// <param name="resourceAsset"></param>
        /// <returns></returns>
        public Resource GetResource(ResourceAsset resourceAsset)
        {
            return ResourceManager.GetResource(resourceAsset);
        }
        
        /// <summary>
        /// Adds value to the given resource
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="value"></param>
        public void AddResourceValue(ResourceAsset asset, float value)
        {
            Resource resource = ResourceManager.GetResource(asset);
            if (resource == null) return;
        }
        
        /// <summary>
        /// Sets the resource to given value
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="value"></param>
        public void SetResourceValue(ResourceAsset asset, float value)
        {
            Resource resource = ResourceManager.GetResource(asset);
            if (resource == null) return;
            resource.SetValue(value);
        }
        
        /// <summary>
        /// Refreshes the resource
        /// </summary>
        /// <param name="asset"></param>
        public void RefreshResourceValue(ResourceAsset asset)
        {
            Resource resource = ResourceManager.GetResource(asset);
            if (resource == null) return;
            resource.RefreshValue();
        }
        
        /// <summary>
        /// Returns the current value of the resource
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public float GetResourceValue(ResourceAsset asset, float defaultValue = 0)
        {
            Resource resource = ResourceManager.GetResource(asset);
            if (resource == null) return defaultValue;
            return resource.GetValue();
        }
        
        /// <summary>
        /// Returns the max value of the given resource
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public float GetResourceMaxValue(ResourceAsset asset, float defaultValue = 1)
        {
            Resource resource = ResourceManager.GetResource(asset);
            if (resource == null) return defaultValue;
            return resource.GetMaxValue();
        }
        
        #endregion
        
        #region Modifiers
        public void AddModifiers(List<StatModifier> modifiers)
        {
            foreach (var modifier in modifiers)
                ExecuteAddModifier(modifier);
            if (IsSpawned) TargetSetModifiers_Rpc(Owner, GetAllModifiers());
        }

        public void AddModifier(StatModifier modifier)
        {
            ExecuteAddModifier(modifier);
            if (IsSpawned) TargetAddModifier_Rpc(Owner, modifier);
        }

        private void ExecuteAddModifier(StatModifier modifier)
        {
            Modifiers ??= new Dictionary<AttributeAsset, HashSet<StatModifier>>();
            if (!Modifiers.ContainsKey(modifier.AttributeAsset))
                Modifiers.Add(modifier.AttributeAsset, new HashSet<StatModifier>());
            Modifiers[modifier.AttributeAsset].Add(modifier);
            DirtyStat(modifier.AttributeAsset);
        }

        public void RemoveModifiers(List<StatModifier> modifiers)
        {
            foreach (var modifier in modifiers)
                ExecuteRemoveModifier(modifier);
            if (IsSpawned) TargetSetModifiers_Rpc(Owner, GetAllModifiers());
        }
        
        public List<StatModifier> GetModifierByTag(string tag)
        {
            List<StatModifier> taggedModifiers = new List<StatModifier>();
            if (Modifiers == null) return taggedModifiers;
            foreach (var pair in Modifiers)
            {
                foreach (var modifier in pair.Value)
                {
                    if (modifier.ModifierTag == tag)
                    {
                        taggedModifiers.Add(modifier);
                    }
                }
            }

            return taggedModifiers;
        }
        
        /// <summary>
        /// Clears all modifiers. A tag can be given to filter out desired modifiers.
        /// </summary>
        /// <param name="clearByTag">If set to true, modifiers with the given tag will be removed only</param>
        /// <param name="tagToCompare"></param>
        public void ClearModifiers(bool clearByTag = false, string tagToCompare = "")
        {
            ExecuteClearModifiers(clearByTag, tagToCompare);
            if (IsSpawned) TargetClearModifiers_Rpc(Owner, clearByTag, tagToCompare);
        }

        private void ExecuteClearModifiers(bool clearByTag = false, string tagToCompare = "")
        {
            if (Modifiers == null) return;
            var toRemove = new List<StatModifier>();
            foreach (var pair in Modifiers)
                foreach (var modifier in pair.Value)
                {
                    if (clearByTag && modifier.ModifierTag != tagToCompare) continue;
                    toRemove.Add(modifier);
                }
            foreach (var modifier in toRemove)
                ExecuteRemoveModifier(modifier);
        }

        public void RemoveModifier(StatModifier modifier)
        {
            ExecuteRemoveModifier(modifier);
            if (IsSpawned) TargetRemoveModifier_Rpc(Owner, modifier);
        }

        private void ExecuteRemoveModifier(StatModifier modifier)
        {
            if (Modifiers == null || !Modifiers.ContainsKey(modifier.AttributeAsset)) return;
            if (Modifiers[modifier.AttributeAsset].Contains(modifier))
            {
                Modifiers[modifier.AttributeAsset].Remove(modifier);
                DirtyStat(modifier.AttributeAsset);
            }
        }

        private List<StatModifier> GetAllModifiers()
        {
            var result = new List<StatModifier>();
            if (Modifiers == null) return result;
            foreach (var pair in Modifiers)
                foreach (var modifier in pair.Value)
                    result.Add(modifier);
            return result;
        }

        public void DirtyStat(AttributeAsset type)
        {
            DirtiedStats.Enqueue(type);
        }

        private void LateUpdate()
        {
            UpdateStatModifiers(); 
        }

        /// <summary>
        /// Updates the modifier value of dirtied stats
        /// </summary>
       public void UpdateStatModifiers()
        {
            if (DirtiedStats == null || DirtiedStats.Count == 0) return;
            
            // Kuyruk bitene kadar dön
            while (DirtiedStats.Count > 0)
            {
                AttributeAsset type = DirtiedStats.Dequeue();
                
                // Eğer stat haritada yoksa atla
                if (!_statMap.ContainsKey(type.Id)) continue;

                float finalFlat = 0;
                float finalPercentAdd = 1f; // %100 (1.0) ile başlar
                float finalPercentMult = 1f; // Çarpım etkisiz elemanı 1 ile başlar

                // O statın tüm modifierlarını gez
                if (Modifiers.ContainsKey(type))
                {
                    foreach (var statModifier in Modifiers[type])
                    {
                        float val = statModifier.GetValue();

                        switch (statModifier.ModifierType)
                        {
                            case ModifierTypes.Flat:
                                finalFlat += val;
                                break;
                                
                            case ModifierTypes.PercentAdd:
                                // %10 artış için 0.1, %10 azalış için -0.1 gönderilmeli
                                finalPercentAdd += val;
                                break;

                            case ModifierTypes.PercentMult:
                                // 2 katına çıkarmak için 2.0
                                // Yarıya indirmek (%50 debuff) için 0.5 gönderilmeli
                                finalPercentMult *= val;
                                break;
                        }
                    }
                }

                // PercentAdd toplamı 0'ın altına düşerse stat negatif olmasın (oyun kuralına göre değişir)
                finalPercentAdd = Mathf.Max(0, finalPercentAdd);

                Attribute currAttribute = _statMap[type.Id];
                
                // Attribute sınıfına bu değerleri set ediyoruz
                // Not: Attribute sınıfında alanların isimlerini güncellemen gerekebilir
                currAttribute.FlatModifier = finalFlat;
                currAttribute.PercentAddModifier = finalPercentAdd;
                currAttribute.PercentMultModifier = finalPercentMult;
                
                _statMap[type.Id] = currAttribute;
            }
        }
        #endregion

        protected override ActorModuleSerializableData InstantiateState()
        {
            var resourceValues = new Dictionary<string, float>();
            if (ResourceManager != null)
                foreach (var resource in ResourceDefinitions)
                    resourceValues[resource.ResourceAsset.Id] = GetResourceValue(resource.ResourceAsset);

            var attributes = new List<SerializableAttributeDefinition>();
            if (_statMap != null)
                foreach (var pair in _statMap)
                    attributes.Add(new SerializableAttributeDefinition
                    {
                        AttributeId   = pair.Key,
                        BaseValue     = pair.Value.BaseValue,
                        ValuePerRak   = pair.Value.ValuePerRank,
                        ValuePerLevel = pair.Value.ValuePerLevel,
                    });

            return new StatsSerializableData()
            {
                Level          = ActorLevel.CurrentLevel,
                Attributes     = attributes,
                AttributeRanks = _statMap?.ToDictionary(p => p.Key, p => p.Value.Rank)
                                 ?? new Dictionary<string, int>(),
                ResourceValues = resourceValues,
            };
        }
        
        /// <summary>
        /// Loads the state of stats. 
        /// </summary>
        /// <param name="serializableData"></param>
        public void SetStatStates(StatsSerializableData serializableData)
        {
            //Set stat states
            if (serializableData.Attributes != null)
            {
                //Clear all stats
                _statMap = new Dictionary<string, Attribute>();
                foreach (var def in serializableData.Attributes)
                {
                    AttributeAsset attributeAsset = RpgManager.GetAttributeAssetById(def.AttributeId);
                    if (attributeAsset == null) continue;
                    AttributeDefinition attDef = new AttributeDefinition
                    {
                        AttributeAsset = attributeAsset,
                        BaseValue = def.BaseValue,
                        ValuePerRank = def.ValuePerRak,
                        ValuePerLevel = def.ValuePerLevel,
                    };
                    ExecuteSetAttribute(attDef, true);
                }
            }


            if (serializableData.AttributeRanks != null)
                foreach (var pair in serializableData.AttributeRanks)
                    SetAttributeRank(pair.Key, pair.Value);

            ExecuteSetLevel(serializableData.Level);

            if (serializableData.ResourceValues != null)
                foreach (var pair in serializableData.ResourceValues)
                {
                    var def = ResourceDefinitions.Find(r => r.ResourceAsset != null && r.ResourceAsset.Id == pair.Key);
                    if (def.ResourceAsset != null) SetResourceValue(def.ResourceAsset, pair.Value);
                }
        }

        /// <summary>
        /// Resets the level, attribute ranks and overflown experience
        /// </summary>
        public void ResetStats()
        {
            //Reset level
            SetLevel(0);
        
            //Reset stat ranks
            if(_statMap == null) return;
            foreach(var pair in _statMap)
            {
                pair.Value.Rank = 0;
            }
        }

        #region Event Handlers
        public override void OnActorRankSet(int rank)
        {
            foreach (var attribute in _statMap.Values)
            {
                attribute.Rank = rank;
            }
        }
        #endregion

        #region Networking
#if NETWORKING_FISHNET
        // Level is public info — all observers need it (nameplates, UI, etc.)
        [ObserversRpc]
        private void ObserverSetActorLevel_Rpc(int newLevel, int earnedExperience)
        {
            if (IsServerInitialized) return;
            if (earnedExperience > 0) ExecuteAddExperience(earnedExperience);
            else if (newLevel != ActorLevel.CurrentLevel) ExecuteSetLevel(newLevel);
        }

        // Attributes are private — only the owner needs them
        [TargetRpc]
        private void TargetSetStat_Rpc(NetworkConnection conn, AttributeDefinition attributeDefinition, bool insertAttribute)
        {
            if (IsServerInitialized) return;
            ExecuteSetAttribute(attributeDefinition, insertAttribute);
        }

        [TargetRpc]
        private void TargetSetAttributeValue_Rpc(NetworkConnection conn, string attributeId, float value)
        {
            if (IsServerInitialized) return;
            ExecuteSetAttributeValue(attributeId, value);
        }

        // Modifiers are private — only the owner needs them
        [TargetRpc]
        private void TargetSetModifiers_Rpc(NetworkConnection conn, List<StatModifier> modifiers)
        {
            if (IsServerInitialized) return;
            ExecuteClearModifiers();
            foreach (var modifier in modifiers)
                ExecuteAddModifier(modifier);
        }

        [TargetRpc]
        private void TargetAddModifier_Rpc(NetworkConnection conn, StatModifier modifier)
        {
            if (IsServerInitialized) return;
            ExecuteAddModifier(modifier);
        }

        [TargetRpc]
        private void TargetRemoveModifier_Rpc(NetworkConnection conn, StatModifier modifier)
        {
            if (IsServerInitialized) return;
            ExecuteRemoveModifier(modifier);
        }

        [TargetRpc]
        private void TargetClearModifiers_Rpc(NetworkConnection conn, bool clearByTag, string tagToCompare)
        {
            if (IsServerInitialized) return;
            ExecuteClearModifiers(clearByTag, tagToCompare);
        }
#else
        private void ObserverSetActorLevel_Rpc(int newLevel, int earnedExperience) { }
        private void TargetSetStat_Rpc(object conn, AttributeDefinition attributeDefinition, bool insertAttribute) { }
        private void TargetSetAttributeValue_Rpc(object conn, string attributeId, float value) { }
        private void TargetSetModifiers_Rpc(object conn, List<StatModifier> modifiers) { }
        private void TargetAddModifier_Rpc(object conn, StatModifier modifier) { }
        private void TargetRemoveModifier_Rpc(object conn, StatModifier modifier) { }
        private void TargetClearModifiers_Rpc(object conn, bool clearByTag, string tagToCompare) { }
#endif
        #endregion
    }
}