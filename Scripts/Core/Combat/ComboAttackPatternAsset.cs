using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core
{
    [CreateAssetMenu(fileName = "ComboAttackPatternAsset", menuName = "Kuantech/Combat/ComboAttackPattern")]
    public class ComboAttackPatternAsset : ScriptableObject
    {
        public List<AttackPatternAsset> Patterns;

        public ComboAttackPattern GetComboAttackPattern()
        {
            var combo = new ComboAttackPattern();
            if (Patterns == null) return combo;
            foreach (var asset in Patterns)
                if (asset != null)
                    combo.Patterns.Add(asset.GetAttackPattern());
            return combo;
        }
    }
}
