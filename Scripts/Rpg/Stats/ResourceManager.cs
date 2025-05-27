using System.Collections.Generic;
using Kuantech.Utils;

namespace Kuantech.Rpg
{
    public class ResourceManager
    {
        public List<ResourceDefinition> ResourceDefinitions;
        private Dictionary<string, Resource> _resources;
        private StatsModule _parentStatsModule;
        
        public void Initialize(StatsModule parentStatsModule)
        {
            _parentStatsModule = parentStatsModule;
            _resources = new Dictionary<string, Resource>();
            if (ResourceDefinitions != null)
            {
                foreach (var definition in ResourceDefinitions)
                {
                    Resource resource = new Resource();
                    resource.StatsModule = parentStatsModule;
                    resource.ApplyResourceDefinition(definition);
                }
            }
        }
        
        /// <summary>
        /// Ticks all resources based on the tick time.
        /// </summary>
        /// <param name="tickTime"></param>
        public void TickResources(float tickTime)
        {
            foreach (var pair in _resources)
            {
                pair.Value.RegenTick(tickTime);
            }
        }
        
        public Resource GetResource(ResourceAsset asset)
        {
            if (_resources.IsNullOrEmpty()) return null;
            if (_resources.TryGetValue(asset.Id, out Resource resource))
            {
                return resource;
            }

            return null;
        }
        
        public void RefreshResource(ResourceAsset asset)
        {
            Resource resourceToRefresh = GetResource(asset);
            resourceToRefresh.RefreshValue();
        }
        
        /// <summary>
        /// Sets all resources to max value based on their attributes
        /// </summary>
        public void Refresh()
        {
            foreach (var pair in _resources)
            {
                pair.Value.RefreshValue();
            }
        }
    }
}