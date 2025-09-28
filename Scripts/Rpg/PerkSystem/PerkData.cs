using System;
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
    public class PerkData
    {
        [NonSerialized] public int CurrentRank;
        [NonSerialized] public PerkAsset PerkAsset;

        public virtual void Apply()
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
            Apply(); //todo: Is this necessary?
        }
    }
}