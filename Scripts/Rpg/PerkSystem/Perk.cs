using System;
using UnityEngine;

namespace Kuantech.Rpg
{
    [Serializable]
    public abstract class PerkConfig
    {
        
    }
    
    [Serializable]
    public class PerkVariable
    {
        public string Name;
        [SerializeReference] public float BaseValue;
        [SerializeReference] public float ValuePerRank;
        public Color TextColor;
        public bool IsPercentage = false;
        public bool DisplayOnlyBaseValue = false;
        
        public float GetValue(int rank)
        {
            return BaseValue + ValuePerRank * rank;
        }

        public float GetDisplayValue(int rank)
        {
            return DisplayOnlyBaseValue ? BaseValue : GetValue(rank);
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

        public virtual void Initialize(PerkAsset parentAsset)
        {
            PerkAsset = parentAsset;
        }

        public virtual void ApplyToTarget(object target)
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

        public virtual void ClearPerk()
        {
            
        }
    }
}