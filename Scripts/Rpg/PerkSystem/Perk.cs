using System;
using Kuantech.Core;
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
        public float BaseValue;
        public float ValuePerRank;
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
        [NonSerialized] protected Actor Owner;

        public virtual void Initialize(PerkAsset parentAsset)
        {
            PerkAsset = parentAsset;
        }

        /// <summary>Binds the perk to the actor that owns it. Called once, when the perk is first acquired.</summary>
        public virtual void Bind(Actor owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// Applies (or re-applies) the perk's effect for the current rank. Called on acquire and on every
        /// rank-up — implementations should refresh their effect (e.g. swap a stat modifier, bump a skill rank).
        /// </summary>
        public virtual void Apply()
        {
        }

        /// <summary>Fully removes the perk's effect (run reset / clear).</summary>
        public virtual void Remove()
        {
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
            CurrentRank++;
            Apply(); // re-apply for the new rank
        }
    }
}