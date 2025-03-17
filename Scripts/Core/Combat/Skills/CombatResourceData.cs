using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Core.Combat
{
    [CreateAssetMenu(fileName = "CombatResourceData", menuName = "Kuantech/Combat/CombatResourceData", order = 0)]
    public class CombatResourceData : ScriptableObject {
        public string Name;
        public string Id;
        [FormerlySerializedAs("MaxValueAttribute")] public StatAttributeAsset maxValueAttributeAsset;
        [FormerlySerializedAs("RegenAttribute")] public StatAttributeAsset regenAttributeAsset;
    }
}