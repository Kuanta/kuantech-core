using Kuantech.Rpg;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Core.Combat
{
    [CreateAssetMenu(fileName = "CombatResourceData", menuName = "Kuantech/Combat/CombatResourceData", order = 0)]
    public class CombatResourceData : ScriptableObject {
        public string Name;
        public string Id;
        [FormerlySerializedAs("MaxValueAttribute")] public AttributeAsset maxValueAttributeAsset;
        [FormerlySerializedAs("RegenAttribute")] public AttributeAsset regenAttributeAsset;
    }
}