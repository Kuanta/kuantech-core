using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public class StatsState : ActorModuleState
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
    public class Stat
    {
        public StatAttribute Attribute;
        [Tooltip("Value at Rank 0 and Level 0")]
        public float BaseValue;
        [Tooltip("Value gained every rank")]
        public float ValuePerRank;
        [Tooltip("Value gained every level")]
        public float ValuePerLevel;
        [Tooltip("Lower and upper boundaries for the attribute")]

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
        public List<Stat> Stats;
        private Dictionary<string, Stat> _statMap;
        public static float LevelFormulaX = 0.4f;

        //Level
        public int CurrentLevel = 0;
        public int OverflowExperience = 0; //Overflow experience is TotalExperience - ExperienceToCurrentLevel
        public int RequiredExperienceToNextLevel = 0;

        //Events
        public EventHandler<int> LevelUpEvent;
        public EventHandler ExperienceEarnedEvent;

        public override void Initialize()
        {
            base.Initialize();
            _statMap = new Dictionary<string, Stat>();
            foreach (var stat in Stats)
            {
                _statMap[stat.Attribute.Id] = stat;
            }
        }

        public override void LoadState(ActorModuleState state)
        {
            base.LoadState(state);
            StatsState statsState = state as StatsState;
            SetStatStates(statsState);
        }

        #region Attributes
        public float GetAttributeValue(StatAttribute attribute)
        {
            if(attribute == null)
            {
                Debug.LogError("Trying to get a null attribute");
                return 0f;
            }
            return GetAttributeValue(attribute.Id);
        }

        public float GetAttributeValue(string statId)
        {
            if(!_statMap.ContainsKey(statId)) return 0f;
            return _statMap[statId].GetValue(CurrentLevel);
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
        /// <param name="attribute">Attribute object</param>
        /// <returns></returns>
        public int GetAttributeRank(StatAttribute attribute)
        {
            return GetAttributeRank(attribute.Id);
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
        /// <param name="attribute">Attribute object</param>
        /// <param name="amountToIncrease">Amount to increase</param>
        public void IncreaseAttributeRank(StatAttribute attribute, int amountToIncrease)
        {
            IncreaseAttributeRank(attribute.Id, amountToIncrease);
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

        protected override ActorModuleState InstantiateState()
        {
            return new StatsState(){
                Level = 0,
                OverflowExperience = 0,
                AttributeRanks = new Dictionary<string, int>(),
            };
        }
        
        /// <summary>
        /// Loads the state of stats. 
        /// </summary>
        /// <param name="state"></param>
        public void SetStatStates(StatsState state)
        {
            if(state.AttributeRanks != null)
            {
                foreach (var pair in state.AttributeRanks)
                {
                    SetAttributeRank(pair.Key, pair.Value);
                }
            }
            CurrentLevel = state.Level;
            OverflowExperience = state.OverflowExperience;
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