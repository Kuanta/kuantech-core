using UnityEngine;

namespace Kuantech.Core
{
    [CreateAssetMenu(fileName = "AttackPatternAsset", menuName = "Kuantech/Combat/AttackPattern")]
    public class AttackPatternAsset : ScriptableObject
    {
        public AttackPattern Template;

        public AttackPattern GetAttackPattern() => Template.Clone();
    }
}
