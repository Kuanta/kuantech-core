using Kuantech.Core;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Midcore
{
    
    /// <summary>
    /// A progressable that represents an attribute upgrade
    /// </summary>
    [CreateAssetMenu(fileName = "TraitUpgradeProgressable", menuName = "Kuantech/Midcore/Trait Upgrade Progressable")]
    public class TraitUpgradeProgressable : ProgressableDataAsset
    {
        public StatModifierData ModifierData;

        public virtual bool CanBeAppliedToActor(Actor actor)
        {
            return true;
        }
        
        /// <summary>
        /// Applies the modifier to the actor
        /// </summary>
        /// <param name="actor"></param>
        public void ApplyToActor(Actor actor)
        {
            int rank = GetUpgradeRank();
            if (rank < 0) return;
            if (actor == null || !CanBeAppliedToActor(actor)) return;
            StatsModule statsModule = actor.GetModule<StatsModule>();
            if (statsModule != null && ModifierData.Stat != null)
            {
                StatModifier modifier = new StatModifier(ModifierData);
                modifier.Level = rank;
                statsModule.AddModifier(modifier);
            }
        }

        public int GetUpgradeRank()
        {
            return ProgressionManager.GetCurrentRank(this);
        }

        public override string GetName()
        {
            if (ModifierData.Stat != null)
            {
                float upgradeValue = ModifierData.LevelToValueFactor;
                return $"{Name} +{upgradeValue}";

            }

            return Name;
        }

        public string GetName(int rank)
        {
            if (ModifierData.Stat != null)
            {
                float upgradeValue = rank == 0 ? ModifierData.BaseValue : ModifierData.LevelToValueFactor;
                return $"{Name} +{upgradeValue}";
            }
            return Name;
        }
    }
}