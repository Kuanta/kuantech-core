using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// Entry that matches a sub-upgrade with an attribute.
    /// </summary>
    [Serializable]
    public struct ModifierUpgradesEntry
    {
        public ProgressableDataAsset ParentAsset;
        public ProgressableDataAsset SubUpgradeAsset;
        public AttributeAsset AttributeAsset;
        public float BaseValue;
        public float LevelToValueFactor;
    }
    
    public class ProgressionHandler : ActorModule
    {
        [Tooltip("Progressable that corresponds to actor level")] 
        public ProgressableDataAsset ActorProgressableAsset;
        
        [Tooltip("List of progressables that corresponds to stat modifiers")]
        public List<ModifierUpgradesEntry> ModifierUpgrades;

        private StatsModule _statsModule;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            
            _statsModule = Actor.GetModule<StatsModule>();
            
            //Get level
            _statsModule.SetLevel(GetActorLevel());
            
            //Set upgrades
            SetProgressableModifiers();
            
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
        
        /// <summary>
        /// Sets the modifiers from progressables
        /// </summary>
        private void SetProgressableModifiers()
        {
            foreach (var entry in ModifierUpgrades)
            {
                int rank = ProgressionManager.GetCurrentRank(entry.ParentAsset, entry.SubUpgradeAsset);
                StatModifier modifier = new StatModifier()
                {
                    AttributeAsset = entry.AttributeAsset,
                    BaseValue = entry.BaseValue,
                    Level = rank,
                    LevelToValueFactor = entry.LevelToValueFactor,
                    ModifierType = ModifierTypes.Addition,
                };
                _statsModule.AddModifier(modifier);
            }
        }
    }
}