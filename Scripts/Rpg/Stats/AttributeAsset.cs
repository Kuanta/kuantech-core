using UnityEngine;

namespace Kuantech.Rpg
{
    [CreateAssetMenu(fileName = "AttributeAsset", menuName = "Kuantech/Rpg/StatAttribute")]
    public class AttributeAsset : ScriptableObject {
        public string Id;
        public string Name;
        public Sprite Icon;
        public string Description;
    }
}