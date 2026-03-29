using System;
using Kuantech.Core;
using Kuantech.Rpg;
using Kuantech.Inventory;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    /// <summary>
    /// A reference to a resource data object
    /// </summary>
    [Serializable]
    public class ResourceDataReference : IdReference
    {
        private ResourceData _resourceData;
        public ResourceData GetResourceData()
        {
            //todo: We can get resource data from arcade idle manager too
            if (_resourceData == null)
            {
                _resourceData = ArcadeIdleManager.GetResourceData(GetId());
            }
            return _resourceData;
        }
        
        /// <summary>
        /// Returns resource icon
        /// </summary>
        /// <returns></returns>
        public Sprite GetResourceIcon()
        {
            ResourceData rd = GetResourceData();
            return AssetCollection.GetSprite(rd.IconId);
        }

        public ResourceVisual GetResourceVisual()
        {
            ResourceData rd = GetResourceData();
            if (rd == null) return null;
            return rd.GetResourceVisual();

        }
    }
}