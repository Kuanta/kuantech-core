using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
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
        public Dictionary<string, int> AttributeRanks;
    }
    
    public class StatsModule : ActorModule
    {
        [Header("Stats")]
        public List<AttributeDefinition> Stats;
        private Dictionary<string, Attribute> _statMap;
        public static float LevelFormulaX = 0.4f;

        [Header("Resources")] public List<ResourceDefinition> ResourceDefinitions;
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

        public void ApplyStatsTable(List<AttributeDefinition> defaultAttributes)
        {
            if (defaultAttributes.IsNullOrEmpty()) return;
            foreach(var attributeDefinition in defaultAttributes)
            {
                Attribute attribute = new Attribute();
                attribute.ApplyAttributeDefinition(attributeDefinition);
                _statMap[attribute.attributeAsset.Id] = attribute;
            }
        }
        
        public override void LoadState(ActorModuleSerializableData serializableData)
        {
            base.LoadState(serializableData);
            StatsSerializableData statsSerializableData = serializableData as StatsSerializableData;
            SetStatStates(statsSerializableData);
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
            int currentLevel = GetActorLevel();
            ActorLevel.AddValue(experience);
            int newLevel = ActorLevel.CurrentLevel;
            if (newLevel > currentLevel)
            {
                LevelChangeEvent?.Invoke(newLevel);
            }

            //Fire the event so subscribers handle changed experience case
            ExperienceEarnedEvent?.Invoke();
        }

        /// <summary>
        /// Sets the level of the player
        /// </summary>
        /// <param name="level"></param>
        public void SetLevel(int level)
        {
            ActorLevel.SetLevel(level);
            if(ResourceManager != null) ResourceManager.Refresh();
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
                Debug.LogError("Trying to get a null attribute");
                return 0f;
            }
            return GetAttributeValue(attributeAsset.Id);
        }

        public float GetAttributeValue(string statId)
        {
            Attribute att = GetAttribute(statId);
            return att.GetValue(GetActorLevel());
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
            {
                AddModifier(modifier);
            }
        }

        public void AddModifier(StatModifier modifier)
        {
            Modifiers ??= new Dictionary<AttributeAsset, HashSet<StatModifier>>();

            if (!Modifiers.ContainsKey(modifier.AttributeAsset))
            {
                Modifiers.Add(modifier.AttributeAsset, new HashSet<StatModifier>());
            }

            Modifiers[modifier.AttributeAsset].Add(modifier);
            DirtyStat(modifier.AttributeAsset);

        }

        public void RemoveModifiers(List<StatModifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                RemoveModifier(modifier);
            }
        }

        /// <summary>
        /// Clears all modifiers. A tag can be given to filter out desired modifiers.
        /// </summary>
        /// <param name="clearByTag">If set to true, modifiers with the given tag will be removed only</param>
        /// <param name="tagToCompare"></param>
        public void ClearModifiers(bool clearByTag = false, string tagToCompare = "")
        {
            if (Modifiers == null) return;
            HashSet<StatModifier> allModifiers = new HashSet<StatModifier>();
            foreach (var pair in Modifiers)
            {
                foreach (var modifier in pair.Value)
                {
                    if (clearByTag && modifier.ModifierTag != tagToCompare) continue;
                    allModifiers.Add(modifier);
                }
            }
            RemoveModifiers(allModifiers.ToList());
        }

        public void RemoveModifier(StatModifier modifier)
        {
            if (Modifiers == null || !Modifiers.ContainsKey(modifier.AttributeAsset)) return;
            if (Modifiers[modifier.AttributeAsset].Contains(modifier))
            {
                Modifiers[modifier.AttributeAsset].Remove(modifier);
                DirtyStat(modifier.AttributeAsset);
            }
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
            AttributeAsset type = DirtiedStats.Dequeue();
            while (type != null)
            {
                float additionMultipliersSum = 0;
                float multiplicationModifiersSum = 1;
                //Handle Stat type
                foreach (var statModifier in Modifiers[type])
                {
                    if (statModifier.ModifierType == ModifierTypes.Addition)
                    {
                        additionMultipliersSum += statModifier.GetValue();
                    }
                    else if (statModifier.ModifierType == ModifierTypes.Multiplication)
                    {
                        multiplicationModifiersSum += statModifier.GetValue();
                    }

                }

                multiplicationModifiersSum = Mathf.Max(0.1f, multiplicationModifiersSum);
                try
                {
                    if (!_statMap.ContainsKey(type.Id))
                    {
                        Debug.LogWarning($"Trying to set value of {type.ToString()} while {name} doesn't have a field for it");
                    }
                    else
                    {
                        Attribute currAttribute = _statMap[type.Id];
                        currAttribute.AdditionModifier = additionMultipliersSum;
                        currAttribute.MultiplicationModifier = multiplicationModifiersSum;
                        _statMap[type.Id] = currAttribute;
                    }

                    if (DirtiedStats.IsNullOrEmpty())
                    {
                        break;
                    }
                    type = DirtiedStats.Dequeue();
                }
                catch (KeyNotFoundException e)
                {
                    Debug.LogError($"Key {type} somehow not in stats");
                    type = DirtiedStats.Dequeue();
                }
            }

        }
        #endregion



        protected override ActorModuleSerializableData InstantiateState()
        {
            return new StatsSerializableData(){
                Level = 0,
                AttributeRanks = new Dictionary<string, int>(),
            };
        }
        
        /// <summary>
        /// Loads the state of stats. 
        /// </summary>
        /// <param name="serializableData"></param>
        public void SetStatStates(StatsSerializableData serializableData)
        {
            if(serializableData.AttributeRanks != null)
            {
                foreach (var pair in serializableData.AttributeRanks)
                {
                    SetAttributeRank(pair.Key, pair.Value);
                }
            }

            SetLevel(serializableData.Level);
            //todo: Overflow
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
    }
}