using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Kuantech.Core
{
    public class WorldZone : MonoBehaviour
    {
        //Runtime
        private bool _active;
        public bool IsActive => _active;

        private List<WorldZoneElement> _zoneElements = new List<WorldZoneElement>();
        private readonly Dictionary<Type, List<WorldZoneElement>> _elementsByType = new Dictionary<Type, List<WorldZoneElement>>();

        public void Initialize()
        {
            DetectZoneElements();
            foreach(var zoneElement in _zoneElements)
            {
                zoneElement.Initialize(this);
            }
        }

        private void DetectZoneElements()
        {
            _zoneElements = GetComponentsInChildren<WorldZoneElement>().ToList();

            // Index by exact type so elements can be fetched like Actor.GetModule.
            _elementsByType.Clear();
            foreach (var element in _zoneElements)
            {
                Type type = element.GetType();
                if (!_elementsByType.TryGetValue(type, out var list))
                {
                    list = new List<WorldZoneElement>();
                    _elementsByType[type] = list;
                }
                list.Add(element);
            }
        }

        #region Element Access

        /// <summary>
        /// Returns the first zone element of type T, or null. Mirrors <see cref="Actor.GetModule{T}"/>.
        /// Exact-type match, like GetModule — query with the concrete element type.
        /// </summary>
        public T GetZoneElementByType<T>() where T : WorldZoneElement
        {
            if (_elementsByType.TryGetValue(typeof(T), out var list) && list.Count > 0)
                return list[0] as T;
            return null;
        }

        /// <summary>
        /// Returns all zone elements of type T (empty list if none). Mirrors <see cref="Actor.GetModules{T}"/>.
        /// </summary>
        public List<T> GetZoneElementsByType<T>() where T : WorldZoneElement
        {
            if (_elementsByType.TryGetValue(typeof(T), out var list))
                return list.Cast<T>().ToList();
            return new List<T>();
        }

        #endregion

        #region Lifecycle

        public void ActivateZone()
        {
            foreach(var zoneElement in _zoneElements)
            {
                zoneElement.OnZoneActivated();
            }
            _active =  true;
        }

        public void DeactivateZone()
        {
            foreach(var zoneElement in _zoneElements)
            {
                zoneElement.OnZoneDeactivated();
            }
            _active = false;
        }
        public void CleanupZone()
        {
            foreach(var zoneElement in _zoneElements)
            {
                zoneElement.CleanupZone();
            }
        }
        #endregion
    }
}