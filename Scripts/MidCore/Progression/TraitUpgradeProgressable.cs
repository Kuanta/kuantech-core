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
    }
}