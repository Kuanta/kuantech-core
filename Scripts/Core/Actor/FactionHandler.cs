using System;
using System.Collections.Generic;

namespace Kuantech.Core
{
    [Serializable]
    public class FactionHandler
    {
        public enum FactionType
        {
            Neutral = 0,
            Enemy = 2,
            Ally = 3,
            Same = 4,
        }

        public int BelongingFaction;
        public List<int> AlliedFactions = new List<int>();
        public List<int> EnemyFactions = new List<int>();
        
        /// <summary>
        /// Returns enemy factions.
        /// </summary>
        /// <returns></returns>
        public List<int> GetEnemyFactions()
        {
            return EnemyFactions;
        }
        
        /// <summary>
        /// Returns allied factions. Doesn't include same faction
        /// </summary>
        /// <returns></returns>
        public List<int> GetAlliedFactions()
        {
            return AlliedFactions;
        }
        
        public FactionType GetFactionRelation(Actor other)
        {
            if (BelongingFaction == other.FactionHandler.BelongingFaction) return FactionType.Same; //They belong in same faction
            if (AlliedFactions.Contains(other.FactionHandler.BelongingFaction)) return FactionType.Ally;
            if (EnemyFactions.Contains(other.FactionHandler.BelongingFaction)) return FactionType.Enemy;
            return FactionType.Neutral;
        }
    }
}