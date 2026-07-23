using System;
using Kuantech.Core;
using Kuantech.Rpg.Skills;

namespace Kuantech.Rpg
{
    [Serializable]
    public struct PerkStateData
    {
        public int Rank;
        public string PerkId;
    }

    [Serializable]
    public abstract class PerkConfig
    {
        /// <summary>
        /// Lets a config expose variables it does not own, so the perk description can print numbers that
        /// live somewhere else — e.g. a skill-granting perk resolving them from the granted skill's own
        /// variables, so the values are balanced in exactly one place. Return false if unknown.
        /// </summary>
        public virtual bool TryGetVariable(string variableId, out SkillVariableData variable)
        {
            variable = null;
            return false;
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