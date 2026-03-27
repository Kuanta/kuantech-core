using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.World
{
    public class Zone : MonoBehaviour
    {
        public string ZoneId;
        public string ZoneType;

        [Range(0f, 1f)]
        public float DangerLevel;

        public Region Region { get; private set; }

        private readonly List<IZoneElement> _elements = new();

        public void Initialize(Region parentRegion)
        {
            Region = parentRegion;

            _elements.Clear();
            GetComponentsInChildren(true, _elements);

            foreach (var element in _elements)
                element.Initialize(this);
        }

        public void Activate()
        {
            foreach (var element in _elements)
                element.OnZoneActivated();
        }

        public void Deactivate()
        {
            foreach (var element in _elements)
                element.OnZoneDeactivated();
        }

        public IReadOnlyList<IZoneElement> Elements => _elements;
    }
}
