using System;
using System.Collections.Generic;
using System.Linq;
using DTT.Utils.Extensions;
using Kuantech.Inventory;
using Kuantech.Inventory.Items;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public enum StatTypes
    {
        MaxHealth,
        HealthRegeneration,
        MaxEnergy,
        EnergyRegeneration,
        Strength,
        Dexterity,
        Intelligence,
        MovementSpeed,
        Armor,
        DamageBonus,
        RangeBonus,
        None,
    }

    public enum ModifierTypes
    {
        Addition,
        Multiplication,
    }

    [Serializable]
    public struct StatModifierData
    {
        public StatTypes StatType;
        public float BaseValue;
        public float LevelToValueFactor;
        public ModifierTypes ModifierType;
        public bool IsPercentage;
    }
    
    [Serializable]
    public class StatModifier
    {
        public int Level = 0; //Required for items
        public StatTypes StatType;
        public float BaseValue;
        public float LevelToValueFactor = 1;
        public StatModifier(){}
        public StatModifier(StatModifierData data)
        {
            BaseValue = data.BaseValue;
            LevelToValueFactor = data.LevelToValueFactor;
            ModifierType = data.ModifierType;
            StatType = data.StatType;
        }
        public float GetValue()
        {
            return BaseValue + LevelToValueFactor * Level * Math.Sign(BaseValue);
        }
        public ModifierTypes ModifierType;
    }

    [Serializable]
    public class CombatModifier
    {
        
    }
    
    [Serializable]
    public struct Stat
    {
        public float BaseValue;
        public float LevelMultiplier;
        public float MultiplicationModifier;
        public float AdditionModifier;
        public float MinValue;
    }
    
    [Serializable]
    public class StatDictionary : SerializableDictionary<StatTypes, Stat> { }
    
    [Serializable]
    public class ModifierDataDictionary : SerializableDictionary<StatTypes, StatModifierData> { }
    
    [Serializable]
    public class StatsModule : Module
    {
        public int Level = 0;
        public int OverflowExperience = 0; //Overflow experience is TotalExperience - ExperienceToCurrentLevel
        public int RequiredExperienceToNextLevel = 0;
        
        public EventHandler LevelUpEvent;
        
        public StatDictionary Stats = new StatDictionary()
        {
            [StatTypes.Strength] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
            [StatTypes.Intelligence] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
            [StatTypes.Dexterity] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
            [StatTypes.EnergyRegeneration] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
            [StatTypes.HealthRegeneration] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
            [StatTypes.EnergyRegeneration] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
            [StatTypes.MaxEnergy] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
            [StatTypes.MaxHealth] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
            [StatTypes.MovementSpeed] = new Stat(){BaseValue = 0, LevelMultiplier = 0},
            [StatTypes.Armor] = new Stat(){BaseValue = 0, LevelMultiplier = 0},
            [StatTypes.RangeBonus] = new Stat(){BaseValue = 0, LevelMultiplier = 0},
            [StatTypes.DamageBonus] = new Stat(){BaseValue = 0, LevelMultiplier = 0},
        };
        
        [NonSerialized] private Dictionary<StatTypes, HashSet<StatModifier>> Modifiers;
        [NonSerialized] private Queue<StatTypes> DirtiedStats = new Queue<StatTypes>();

        public void CopyFrom(StatsModule statsModule)
        {
            Level = statsModule.Level;
            OverflowExperience = statsModule.OverflowExperience;
            RequiredExperienceToNextLevel = statsModule.RequiredExperienceToNextLevel;
            Stats = statsModule.Stats;
        }
        public override void OnModulesInitialized(object sender, EventArgs args)
        {
            InventoryModule invMod = (InventoryModule)Actor.GetModuleByType(typeof(InventoryModule));
            if (invMod == null) return;
            invMod.ItemEquipEvent += ItemEquippedHandler;
            invMod.ItemUnequipEvent += ItemUnequippedHandler;
        }

        public void ItemEquippedHandler(object sender, Item item)
        {
            if (item.StateData.StatModifiers != null)
            {
                AddModifiers(item.StateData.StatModifiers.Values.ToList());
            }
            
            //Apply armor value
            if (!(item is Armor armor)) return;
            Stat armorStat = Stats[StatTypes.Armor];
            armorStat.AdditionModifier +=  armor.GetArmorRating();
            Stats[StatTypes.Armor] = armorStat;
        }

        public void ItemUnequippedHandler(object sender, Item item)
        {
            if (item.StateData.StatModifiers != null)
            {
                RemoveModifiers(item.StateData.StatModifiers.Values.ToList());
            }

            if (!(item is Armor armor)) return;
            Stat armorStat = Stats[StatTypes.Armor];
            armorStat.AdditionModifier -=  armor.GetArmorRating();
            Stats[StatTypes.Armor] = armorStat;
        }
        
        private void Update()
        {
            UpdateStatModifiers();
        }
        
        public void AddModifiers(List<StatModifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                AddModifier(modifier);
            }    
        }
        
        public void AddModifier(StatModifier modifier)
        {
            Modifiers ??= new Dictionary<StatTypes, HashSet<StatModifier>>();

            if (modifier.StatType == StatTypes.MaxHealth)
            {
                float modifierValue = modifier.GetValue();
                if (modifier.ModifierType == ModifierTypes.Addition)
                {
                    Actor.Health += modifierValue;
                }
                else
                {
                    Actor.Health += GetBaseStat(StatTypes.MaxHealth) * modifierValue;
                }
            }else if (modifier.StatType == StatTypes.MaxEnergy)
            {
                float modifierValue = modifier.GetValue();
                if (modifier.ModifierType == ModifierTypes.Addition)
                {
                    Actor.Energy += modifierValue;
                }
                else
                {
                    Actor.Energy += GetBaseStat(StatTypes.MaxHealth) * modifierValue;
                }
            }
            
            if (!Modifiers.ContainsKey(modifier.StatType))
            {
                Modifiers.Add(modifier.StatType, new HashSet<StatModifier>());
            }

            Modifiers[modifier.StatType].Add(modifier);
            DirtyStat(modifier.StatType);

        }
        
        public void RemoveModifiers(List<StatModifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                RemoveModifier(modifier);
            }    
        }
        
        public void RemoveModifier(StatModifier modifier)
        {
            if(Modifiers == null) return;
            if (modifier.StatType == StatTypes.MaxHealth)
            {
                float modifierValue = modifier.GetValue();
                if (modifier.ModifierType == ModifierTypes.Addition)
                {
                    Actor.Health -= modifierValue;
                }
                else
                {
                    Actor.Health -= GetBaseStat(StatTypes.MaxHealth) * modifierValue;
                }
            }else if (modifier.StatType == StatTypes.MaxEnergy)
            {
                float modifierValue = modifier.GetValue();
                if (modifier.ModifierType == ModifierTypes.Addition)
                {
                    Actor.Energy -= modifierValue;
                }
                else
                {
                    Actor.Energy -= GetBaseStat(StatTypes.MaxHealth) * modifierValue;
                }
            }
            if (Modifiers[modifier.StatType].Contains(modifier))
            {
                Modifiers[modifier.StatType].Remove(modifier);
                DirtyStat(modifier.StatType);
            }
        }

        private void DirtyStat(StatTypes statType)
        {
            DirtiedStats.Enqueue(statType);
        }
        
        /// <summary>
        /// Updates the modifier value of dirtied stats
        /// </summary>
        public void UpdateStatModifiers()
        {
            if (DirtiedStats == null || DirtiedStats.Count == 0) return;
            StatTypes statType = DirtiedStats.Dequeue();
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
                    else if(statModifier.ModifierType == ModifierTypes.Multiplication)
                    {
                        multiplicationModifiersSum += statModifier.GetValue();
                    }
                    
                }

                try
                {
                    if (!Stats.ContainsKey(statType))
                    {
                        Debug.LogWarning($"Trying to set value of {statType.ToString()} while {name} doesn't have a field for it");
                    }
                    else
                    {
                        Stat currStat = Stats[statType];
                        currStat.AdditionModifier = additionMultipliersSum;
                        currStat.MultiplicationModifier = multiplicationModifiersSum;
                        Stats[statType] = currStat;
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

        
        #region Getters
        
        /// <summary>
        /// Returns the final value for a stat
        /// </summary>
        /// <param name="statType"></param>
        /// <returns></returns>
        public float GetStat(StatTypes statType)
        {
            if (!Stats.ContainsKey(statType)) return 0;
            if (statType == StatTypes.None) return 0;
            Stat desiredStat = Stats[statType];
            float baseValue = GetBaseStat(statType);
            float finalValue = baseValue * (1 + desiredStat.MultiplicationModifier) + desiredStat.AdditionModifier;
            //Exceptions
            if (statType == StatTypes.MovementSpeed)
            {
               finalValue *= GetWeightFactor(0.5f); //todo: A better way can be found for movement speed
            }

            return Mathf.Max(finalValue, Stats[statType].MinValue);
        }

        public float GetEncumbrance()
        {
            InventoryModule invMod = (InventoryModule)Actor.GetModuleByType(typeof(InventoryModule));
            if (invMod == null) return 0f;
            return invMod.equipment.Encumbrance;
        }

        public float GetWeightFactor(float minValue = 0f)
        {
            float value = 1f - GetEncumbrance() / Config.MAX_ENCUMBRANCE;
            value = Mathf.Clamp(value, minValue, 1f);
            return value;
        }
        
        /// <summary>
        /// Returns the non-modified value of the stat (still considering the level)
        /// </summary>
        /// <param name="statType"></param>
        /// <returns></returns>
        public float GetBaseStat(StatTypes statType)
        {
            if (statType == StatTypes.None) return -1;
            Stat desiredStat = Stats[statType]; 
            return desiredStat.BaseValue + desiredStat.LevelMultiplier * Mathf.Max(Level-1, 0);
        }
        #endregion

        #region Level
        /// <summary>
        /// Increase the experience
        /// </summary>
        /// <param name="experience"></param>
        public void EarnExperience(int experience)
        {
            OverflowExperience += experience;
            while (OverflowExperience >= RequiredExperienceToNextLevel)
            {
                LevelUpEvent?.Invoke(this, EventArgs.Empty);
                OverflowExperience = OverflowExperience - RequiredExperienceToNextLevel;
                Level++;
                RequiredExperienceToNextLevel = GetRequiredExperienceForLevel(Level+1);
                
                //Refresh
                Actor.SetHealth(1f);
                Actor.SetEnergy(1f);
            }

        }
        
        /// <summary>
        /// Removes experience and de-levels if necessary
        /// </summary>
        /// <param name="experience"></param>
        public void RemoveExperience(int experience)
        {
            CalculateLevelOnExperienceRemove(experience, out var newLevel, out var newOverflowExperience);
            Level = newLevel;
            OverflowExperience = newOverflowExperience;
        }

        public void CalculateLevelOnExperienceRemove(int experience, out int newLevel,
            out int newOverflowExperience)
        {
            newLevel = Level;
            newOverflowExperience = OverflowExperience;
            while (experience > 0)
            {
                if (experience > newOverflowExperience)
                {
                    newLevel--;
                    experience -= newOverflowExperience;
                    newOverflowExperience = GetRequiredExperienceForLevel(newLevel + 1);
                }
                else
                {
                    newOverflowExperience -= experience;
                    return;
                }
            }

            newLevel = Mathf.Max(0, newLevel);
            newOverflowExperience = Mathf.Max(0, newOverflowExperience);
        }
        
        public int GetLevel()
        {
            return Level;
        }

        public int GetOverflowExperience()
        {
            return OverflowExperience;
        }

        /// <summary>
        /// Sets the level and adjust the experience accordingly
        /// </summary>
        /// <param name="level"></param>
        /// <param name="overflowExperience"></param>
        public void SetLevel(int level, int overflowExperience = 0)
        {
            level = Mathf.Max(1, level);
            Level = level;
            OverflowExperience = overflowExperience;
            RequiredExperienceToNextLevel = GetRequiredExperienceForLevel(level + 1);
            Actor.SetHealth(1f);
            Actor.SetEnergy(1f);
        }
        
        /// <summary>
        /// Returns the percentage of exp that is required for next level
        /// </summary>
        /// <returns></returns>
        public float GetPercentageExperience()
        {
            float lowerLimit = GetExperienceForLevel(Level - 1);
            float upperLimit = GetExperienceForLevel(Level);

            return (OverflowExperience - lowerLimit) / (upperLimit - lowerLimit);
        }
        
        /// <summary>
        /// Returns the normalized value of the given overflow experience
        /// </summary>
        /// <param name="currentLevel"></param>
        /// <param name="overflowExperience"></param>
        /// <returns></returns>
        public static float GetNormalizedOverflowExperience(int currentLevel, int overflowExperience)
        {
            return overflowExperience / (float)GetRequiredExperienceForLevel(currentLevel + 1);
        }
        
        /// <summary>
        /// Returns required amount of experience to reach next level.
        /// </summary>
        /// <param name="currentLevel"></param>
        /// <returns></returns>
        public static int GetRequiredExperienceForLevel(int currentLevel)
        {
            if (currentLevel == 0) return 0;
            return GetExperienceForLevel(currentLevel) - (int)Mathf.Max(GetExperienceForLevel(currentLevel-1), 0);
        }
        
        public static int GetExperienceForLevel(int level)
        {
            return (int)Mathf.Floor(Mathf.Pow((level) / Config.LEVEL_FORMULA_X, 2));
        }

        public static void GetExperienceEarnResults(int currentLevel, int currentOverflowExperience, int earnedExperience, out int resultingLevel,
            out int resultingOverflowExperience)
        {
            resultingOverflowExperience = 0;
            resultingLevel = currentLevel;

            int experienceToNextLevel = GetRequiredExperienceForLevel(currentLevel + 1);

            int newOverflowExperience = currentOverflowExperience + earnedExperience;
            
            if (currentOverflowExperience  + earnedExperience < experienceToNextLevel)
            {
                resultingOverflowExperience = currentOverflowExperience + earnedExperience;
                return;
            }

            while (newOverflowExperience >= experienceToNextLevel)
            {
                resultingLevel++;
                newOverflowExperience -= experienceToNextLevel;
                experienceToNextLevel = GetRequiredExperienceForLevel(resultingLevel + 1);
                resultingOverflowExperience = newOverflowExperience;
            }
                
        }
        #endregion
        
        [Button("Set Default Stats")]
        public void SetDefaultStats()
        {
            Stats = new StatDictionary()
            {
                [StatTypes.Strength] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
                [StatTypes.Intelligence] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
                [StatTypes.Dexterity] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
                [StatTypes.EnergyRegeneration] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
                [StatTypes.HealthRegeneration] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
                [StatTypes.EnergyRegeneration] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
                [StatTypes.MaxEnergy] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
                [StatTypes.MaxHealth] = new Stat(){BaseValue = 0, LevelMultiplier = Config.DEFAULT_LEVEL_TO_STAT_FACTOR},
                [StatTypes.MovementSpeed] = new Stat(){BaseValue = 0, LevelMultiplier = 0},
                [StatTypes.Armor] = new Stat(){BaseValue = 0, LevelMultiplier = 0},
                [StatTypes.RangeBonus] = new Stat(){BaseValue = 0, LevelMultiplier = 0},
                [StatTypes.DamageBonus] = new Stat(){BaseValue = 0, LevelMultiplier = 0},
            };
        }
    }
}