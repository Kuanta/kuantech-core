using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.World
{
    public class World : MonoBehaviour
    {
        private readonly List<Region> _regions = new();

        public virtual void LoadLevel()
        {
            _regions.Clear();
            GetComponentsInChildren(true, _regions);

            foreach (var region in _regions)
                region.Initialize(this);
        }

        public virtual void UnloadLevel()
        {
            foreach (var region in _regions)
                foreach (var zone in region.Zones)
                    zone.Deactivate();
        }

        public IReadOnlyList<Region> Regions => _regions;
    }
}
