using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// A data asset that represents metadata for all sorts of things
    /// </summary>
    [CreateAssetMenu(fileName = "MetadataAsset", menuName = "Kuantech/Data/MetadataAsset")]
    public class MetadataAsset : ScriptableObject
    {
        [SerializeField] protected string Id;
        [SerializeField] protected string Name;
        [SerializeField] protected string Description;
        [SerializeField] protected Sprite Icon;
        public Color MainColor;

        public virtual string GetId()
        {
            return Id;
        }

        public virtual string GetName()
        {
            return Name;
        }
        
        public virtual string GetDescription()
        {
            return Description;
        }

        public virtual Sprite GetIcon()
        {
            return Icon;
        }
    }
}