using Kuantech.Core;
using Kuantech.Core.Combat;
using UnityEngine;

namespace Kuantech.Rpg
{
    [CreateAssetMenu(fileName = "DamageType", menuName = "Kuantech/Rpg/Damage Type")]
    public class DamageType : MetadataAsset
    {
        public ResourceAsset AffectedResource; //Which resource to damage
        public AttributeAsset ResistanceAttribute; //Which attribute provides resistance
        public DamageReductionFormula DamageReductionFormula; //Damage reduction formula for this damage type

    }
}