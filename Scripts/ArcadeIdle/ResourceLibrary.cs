using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{


    public class ResourceLibrary : Singleton<ResourceLibrary>
    {
        [SerializeField] private List<ResourceData> ResourceDataList;
        private Dictionary<string, ResourceData> ResourceDictionary;

        private void Start()
        {
            ResourceDictionary = new Dictionary<string, ResourceData>();
            foreach (var data in ResourceDataList)
            {
                ResourceDictionary[data.ResourceId] = data;
            }
        }

        public ResourceData GetResourceData(string resourceId)
        {
            if (!ResourceDictionary.ContainsKey(resourceId)) return null;
            return ResourceDictionary[resourceId];
        }

        public ResourceVisual GetResourceVisual(string resourceId)
        {
            ResourceData data = GetResourceData(resourceId);
            if (data == null || data.ResourcePrefab == null) return null;
            ResourceVisual visual = GameManager.Instance.Pool.GetObject(data.ResourcePrefab.gameObject).GetComponent<ResourceVisual>();
            visual.ResourceId = resourceId;
            return visual;
        }
    }
}