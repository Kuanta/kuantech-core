using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Rpg;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{

    
    public class ProgressionHandler : ActorModule
    {
        [Tooltip("Progressable that corresponds to actor level")] 
        public ProgressableDataAsset ActorProgressableAsset;
        
        // [Tooltip("List of progressables that corresponds to stat modifiers")]
        // public List<TraitUpgradeProgressable> ModifierUpgrades;

        private StatsModule _statsModule;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            
            _statsModule = Actor.GetModule<StatsModule>();
            
            //Get level
            _statsModule.SetLevel(GetActorLevel());
            
            //Set upgrades
            //SetTraitUpgrades();
            
            //todo: Implement passives and spells
        }

        private int GetActorLevel()
        {
            int level = ProgressionManager.GetCurrentRank(ActorProgressableAsset);
            if (level < 0)
            {
                return 0;
            }

            return level;
        }
        
        // /// <summary>
        // /// Sets the modifiers from progressables
        // /// </summary>
        // private void SetTraitUpgrades()
        // {
        //     if(ModifierUpgrades.IsNullOrEmpty()) return;
        //     foreach (var entry in ModifierUpgrades)
        //     {
        //         int rank = ProgressionManager.GetCurrentRank(entry);
        //         if(rank < 0) continue;
        //         StatModifier modifier = new StatModifier(entry.ModifierData);
        //         modifier.Level = rank;
        //         _statsModule.AddModifier(modifier);
        //     }
        // }
    }
}