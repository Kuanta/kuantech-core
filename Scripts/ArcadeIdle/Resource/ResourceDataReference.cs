using Kuantech.Rpg;
using Kuantech.Rpg.Inventory;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    /// <summary>
    /// A reference to a resource data object
    /// </summary>
    [CreateAssetMenu(fileName = "Resource Data Referernce", menuName = "Kuantech/ArcadeIdle/Resource Data Reference")]
    public class ResourceDataReference : ScriptableObject
    {
        public string ResourceId;

        private ResourceData _resourceData;
        public ResourceData GetResourceData()
        {
            //todo: We can get resource data from arcade idle manager too
            if (_resourceData == null)
            {
                _resourceData = Librarian.GetItemData(ResourceId) as ResourceData;
            }
            return _resourceData;
        }
        
        /// <summary>
        /// Returns resource icon
        /// </summary>
        /// <returns></returns>
        public Sprite GetResourceIcon()
        {
            ItemTemplate template = Librarian.GetItemTemplate(ResourceId);
            if (template == null) return null;
            return template.ItemIcon;
        }

        public ResourceVisual GetResourceVisual()
        {
            ItemData data = Librarian.GetItemData(ResourceId);
            if (data == null) return null;
            if (data is ResourceData rd)
            {
                return rd.GetResourceVisual();
            }
            return null;
        }
    }
}