using UnityEngine;

namespace Kuantech.Core.Combat
{
    [CreateAssetMenu(fileName = "CombatResourceData", menuName = "Kuantech/Combat/CombatResourceData", order = 0)]
    public class CombatResourceData : ScriptableObject {
        public string Name;
        public string Id;
        public StatAttribute MaxValueAttribute;
        public StatAttribute RegenAttribute;
    }
}