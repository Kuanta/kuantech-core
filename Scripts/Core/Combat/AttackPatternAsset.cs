using UnityEngine;

namespace Kuantech.Core
{
    [CreateAssetMenu(fileName = "AttackPatternAsset", menuName = "Kuantech/Combat/AttackPattern")]
    public class AttackPatternAsset : MetadataAsset
    {
        public AttackPattern Template;

        public AttackPattern GetAttackPattern() => Template.Clone();
    }
}
