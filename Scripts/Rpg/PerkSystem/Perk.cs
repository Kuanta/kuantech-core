using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Rpg
{
    [Serializable]
    public class PerkVariable
    {
        public string Name;
        [SerializeReference] public float BaseValue;
        [SerializeReference] public float ValuePerRank;
        public Color TextColor;
        public bool IsPercentage = false;
        
        public float GetValue(int rank)
        {
            return BaseValue + ValuePerRank * rank;
        }
    }
    
    /// <summary>
    /// A perk data
    /// </summary>
    [Serializable]
    public class Perk
    {
        [NonSerialized] public int CurrentRank;
        [NonSerialized] public PerkAsset PerkAsset;
        
        /// <summary>
        /// If perk is added to actor, this will be called to make necessary changes
        /// </summary>
        /// <param name="actor"></param>
        public virtual void ApplyToActor(Actor actor)
        {
            
        }
        
        public virtual void UpdatePerkEffect()
        {
            //Any logic to apply the perk
        }
        
        public int GetCurrentRank()
        {
            return CurrentRank;
        }
        
        public void SetCurrentRank(int rank)
        {
            CurrentRank = rank;
        }

        public void IncreaseRank()
        {
            SetCurrentRank(GetCurrentRank()+1);
            UpdatePerkEffect(); //todo: Is this necessary?
        }
    }
}