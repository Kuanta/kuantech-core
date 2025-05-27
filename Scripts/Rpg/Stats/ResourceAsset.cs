using UnityEngine;

namespace Kuantech.Rpg
{
    [CreateAssetMenu(fileName = "ResourceAsset", menuName = "Kuantech/Rpg/Resource Asset")]
    public class ResourceAsset : ScriptableObject
    {
        public string Id;
        public string Name;
        public Sprite Icon;
        public string Description;
    }
}