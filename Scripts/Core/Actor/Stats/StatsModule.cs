using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Core
{
    [Serializable]
    public class StatDictionary : SerializableDictionary<StatAttributeAsset, Attribute> { }

    [Serializable]
    public class ModifierDataDictionary : SerializableDictionary<StatAttributeAsset, StatModifierData> { }

    [Serializable]
    public class StatsSerializableData : ActorModuleSerializableData
    {
        public int Level;
        public int OverflowExperience;
        public Dictionary<string, int> AttributeRanks;
    }

    /// <summary>
    /// A Stat is a levelable variable. Damage, Range, AttackSpeed can be defined as stats.
    /// Stats are increased by the overall level as well as with their ranks. 
    /// </summary>
    [Serializable]
    public class Attribute
    {
        public StatAttributeAsset attributeAsset;
        [Tooltip("Value at Rank 0 and Level 0")]
        public float BaseValue;
        [Tooltip("Value gained every rank")]
        public float ValuePerRank;
        [Tooltip("Value gained every level")]
        public float ValuePerLevel;
        [Tooltip("Lower and upper boundaries for the attribute")]

        public float MultiplicationModifier;
        public float AdditionModifier;
        public Vector2 Limits;
        public int Rank;
        
        /// <summary>
        /// Calculates the final value of the stat.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public float GetValue(int level)
        {
            float finalValue = BaseValue + Rank * ValuePerRank + level * ValuePerLevel;
            if(Limits.x != 0 && Limits.y != 0)
            {
                finalValue = Mathf.Clamp(finalValue, Limits.x, Limits.y);
            }
            return finalValue;
        }
    }

    public class StatsModule : ActorModule
    {
        [Header("Stats")]
        public List<Attribute> Stats;
        private Dictionary<string, Attribute> _statMap;
        public static float LevelFormulaX = 0.4f;

        //Level
        public int CurrentLevel = 0;
        public int OverflowExperience = 0; //Overflow experience is TotalExperience - ExperienceToCurrentLevel
        public int RequiredExperienceToNextLevel = 0;

        //Events
        public EventHandler<int> LevelUpEvent;
        public EventHandler ExperienceEarnedEvent;

        [NonSerialized] private Dictionary<StatAttributeAsset, HashSet<StatModifier>> Modifiers;
        [NonSerialized] private Queue<StatAttributeAsset> DirtiedStats = new Queue<StatAttributeAsset>();

        public override void Initialize()
        {
            base.Initialize();
            _statMap = new Dictionary<string, Attribute>();
            foreach (var stat in Stats)
            {
                if(stat == null) continue;
                _statMap[stat.attributeAsset.Id] = stat;
            }
        }

        public override void LoadState(ActorModuleSerializableData serializableData)
        {
            base.LoadState(serializableData);
            StatsSerializableData statsSerializableData = serializableData as StatsSerializableData;
            SetStatStates(statsSerializableData);
        }

        #region Attributes

        public Attribute GetAttribute(StatAttributeAsset attributeAsset)
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
        public float GetAttributeMaxValue(StatAttributeAsset attributeAsset)
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
        public float GetAttributeMinValue(StatAttributeAsset attributeAsset)
        {
            Attribute att = GetAttribute(attributeAsset);
            if (att == null) return 0f;
            return att.Limits.x;
        }
        
        public float GetAttributeValue(StatAttributeAsset attributeAsset)
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
            return att.GetValue(CurrentLevel);
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
        public int GetAttributeRank(StatAttributeAsset attributeAsset)
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
        public void IncreaseAttributeRank(StatAttributeAsset attributeAsset, int amountToIncrease)
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
            Modifiers ??= new Dictionary<StatAttributeAsset, HashSet<StatModifier>>();

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

        public void DirtyStat(StatAttributeAsset statType)
        {
            DirtiedStats.Enqueue(statType);
        }

        /// <summary>
        /// Updates the modifier value of dirtied stats
        /// </summary>
        public void UpdateStatModifiers()
        {
            if (DirtiedStats == null || DirtiedStats.Count == 0) return;
            StatAttributeAsset statType = DirtiedStats.Dequeue();
            while (statType != null)
            {
                float additionMultipliersSum = 0;
                float multiplicationModifiersSum = 0;
                //Handle Stat type
                foreach (var statModifier in Modifiers[statType])
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

                try
                {
                    if (!_statMap.ContainsKey(statType.Id))
                    {
                        Debug.LogWarning($"Trying to set value of {statType.ToString()} while {name} doesn't have a field for it");
                    }
                    else
                    {
                        Attribute currAttribute = _statMap[statType.Id];
                        currAttribute.AdditionModifier = additionMultipliersSum;
                        currAttribute.MultiplicationModifier = multiplicationModifiersSum;
                        _statMap[statType.Id] = currAttribute;
                    }

                    if (DirtiedStats.IsNullOrEmpty())
                    {
                        break;
                    }
                    statType = DirtiedStats.Dequeue();
                }
                catch (KeyNotFoundException e)
                {
                    Debug.LogError($"Key {statType} somehow not in stats");
                    statType = DirtiedStats.Dequeue();
                }
            }

        }
        #endregion

        #region Level & Experience
        /// <summary>
        /// Adds experience points to the actor. The actor is leveled up if enough experience is earned
        /// </summary>
        /// <param name="experience"></param>
        public void AddExperience(int experience)
        {
            OverflowExperience += experience;
            RequiredExperienceToNextLevel = GetRequiredExperienceToLevelUp(CurrentLevel + 1);
            //Check if added experience levels up the actor
            while (OverflowExperience >= RequiredExperienceToNextLevel)
            {
                LevelUpEvent?.Invoke(this, CurrentLevel + 1);
                OverflowExperience = OverflowExperience - RequiredExperienceToNextLevel;
                CurrentLevel++;

                //Can the actor level up once more?
                RequiredExperienceToNextLevel = GetRequiredExperienceToLevelUp(CurrentLevel + 1);
            }

            //Fire the event so subscribers handle changed experience case
            ExperienceEarnedEvent?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Sets the level of the player
        /// </summary>
        /// <param name="level"></param>
        public void SetLevel(int level)
        {
            CurrentLevel = level;
            OverflowExperience = 0;
            RequiredExperienceToNextLevel = GetRequiredExperienceToLevelUp(CurrentLevel);
        }

        /// <summary>
        /// Returns the required amount of experience needed to achieve a level from its previous level. 
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int GetRequiredExperienceToLevelUp(int level)
        {
            if (level == 0) return 0;
            return GetExperienceForLevel(level) - (int)Mathf.Max(GetExperienceForLevel(level-1), 0);
        }

        /// <summary>
        /// Returns the total amount of experience an actor must earn from level 0 to this level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int GetExperienceForLevel(int level)
        {
            if(level == 0) return 0;
            return (int)Mathf.Floor(Mathf.Pow((level) / StatsModule.LevelFormulaX, 2));
        }

        /// <summary>
        /// Returns the percentage of exp that is required for next level
        /// </summary>
        /// <returns></returns>
        public float GetPercentageExperience()
        {
            float reqExp = GetRequiredExperienceToLevelUp(CurrentLevel+1);
            if(reqExp == 0) return 0;
            return OverflowExperience / reqExp;
        }
        #endregion

        protected override ActorModuleSerializableData InstantiateState()
        {
            return new StatsSerializableData(){
                Level = 0,
                OverflowExperience = 0,
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
            CurrentLevel = serializableData.Level;
            OverflowExperience = serializableData.OverflowExperience;
        }

        /// <summary>
        /// Resets the level, attribute ranks and overflown experience
        /// </summary>
        public void ResetStats()
        {
            //Reset level
            CurrentLevel = 0;
            OverflowExperience = 0;
            RequiredExperienceToNextLevel = GetRequiredExperienceToLevelUp(1);
        
            //Reset stat ranks
            if(_statMap == null) return;
            foreach(var pair in _statMap)
            {
                pair.Value.Rank = 0;
            }
        }
    }
}