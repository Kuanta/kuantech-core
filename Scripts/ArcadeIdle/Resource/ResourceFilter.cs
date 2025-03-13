using System;
using System.Collections.Generic;
using Kuantech.Utils;

namespace Kuantech.ArcadeIdle
{
    [Serializable]
    public class ResourceFilter
    {
        public List<ResourceDataReference> AllowedResourceReferences;
        public bool AllowAllIfEmpty = true;
        [NonSerialized] public HashSet<ResourceData> AllowedResources;
        public void Initialize()
        {
            AllowedResources = new HashSet<ResourceData>();
            foreach (var allowed in AllowedResourceReferences)
            {
                AllowedResources.Add(allowed.GetResourceData());
            }
        }
        
        /// <summary>
        /// Checks whether resource is allowed
        /// </summary>
        /// <param name="resData"></param>
        /// <returns></returns>
        public bool ResourceAllowed(ResourceData resData)
        {
            if (AllowedResources.IsNullOrEmpty())
            {
                return AllowAllIfEmpty;
            }
            return AllowedResources.Contains(resData);
        }
        
        public bool IsEmpty()
        {
            return AllowedResourceReferences.Count == 0;
        }
    }
}