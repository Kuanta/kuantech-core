using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// A data asset that represents metadata for all sorts of things
    /// </summary>
    [CreateAssetMenu(fileName = "MetadataAsset", menuName = "Kuantech/Data/MetadataAsset")]
    public class MetadataAsset : ScriptableObject
    {
        public string Id;
        public string Name;
        public string Description;
        public Sprite Icon;
        public Color MainColor;

        public string GetId()
        {
            return Id;
        }
    }
}