using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.World
{
    public class Region : MonoBehaviour
    {
        public string RegionId;

        public World World { get; private set; }

        private readonly List<Zone> _zones = new();

        public void Initialize(World parentWorld)
        {
            World = parentWorld;

            _zones.Clear();
            GetComponentsInChildren(true, _zones);

            foreach (var zone in _zones)
                zone.Initialize(this);
        }

        public IReadOnlyList<Zone> Zones => _zones;
    }
}
