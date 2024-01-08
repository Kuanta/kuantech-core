using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ResourceDisplayer : MonoBehaviour
    {
        public enum ResourceVisualizationMethod
        {
            None,
            Stacking, //Stacking with a stacker
            Predefined, //Predefined resource placements
            Percentage, //Different models for the resource node
        }

        [Header("Filter")]
        public List<ResourceData> RejectedResources = new List<ResourceData>();
        public bool DisplaysCurrencies = false;
        
        [Header("Visualization")]
        [SerializeField] private ResourceVisualizationMethod VisualizationMethod;
        [SerializeField] private ResourceStacker ResourceStacker;
        [SerializeField] private List<Transform> ResourcePositions;

        private List<ResourceVisual> _displayedResources;

        public int GetDisplayedResourceCount()
        {
            if(_displayedResources == null) return 0;
            return _displayedResources.Count;
        }
        
        public bool AcceptsResource(ResourceData data)
        {
            if(data.IsCurrency() && !DisplaysCurrencies) return false;
            if (RejectedResources == null) return true;
            return !RejectedResources.Contains(data);
        }

        /// <summary>
        /// Adds 
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="flyToPosition"></param>
        public void AddResourceVisual(ResourceVisual resource, bool flyToPosition = true)
        {
            if(_displayedResources == null) _displayedResources = new List<ResourceVisual>();
            if(!resource.DespawnOnReach || (!flyToPosition && resource.DespawnOnReach)) RegisterVisual(resource); //If resource will be shown on reach, register it beforehand so that _displayedResources list is filled
            resource.transform.SetParent(null);
            resource.ParentDisplayer = this;
            switch (VisualizationMethod)
            {
                case ResourceVisualizationMethod.Stacking:
                    if(ResourceStacker == null)
                    {
                        Debug.LogError($"Resource Stacker is null for {gameObject.name}");
                        return;
                    }
                    ResourceStacker.StackObject(resource, Mathf.Max(_displayedResources.Count - 1, 0), flyToPosition);
                    break;
                default:
                    break;
            }
        }

        public void RegisterVisual(ResourceVisual visual)
        {
            if (_displayedResources == null) _displayedResources = new List<ResourceVisual>();
            _displayedResources.Add(visual);
        }
        
        public ResourceVisual RemoveResourceVisual(string resourceId)
        {
            if (_displayedResources == null)
            {
                Debug.LogError("???");
                return null;
            }
            int indexToRemove = -1;
            for (int i = _displayedResources.Count - 1; i >= 0; --i)
            {
                //Check if the visual is the correct one and not flying
                if (_displayedResources[i].ResourceId != resourceId || _displayedResources[i].IsMoving) continue;
                indexToRemove = i;
                break;
            }

            if (indexToRemove >= 0)
            {
                ResourceVisual resource = _displayedResources[indexToRemove];
                _displayedResources.RemoveAt(indexToRemove);
                
                //Recalculate positions
                RecalculateResourcePositions();
                return resource;
            }

            return null;
        }
        
        public void RemoveGivenResourceVisual(ResourceVisual visual)
        {
            if(_displayedResources == null) return;
            _displayedResources.Remove(visual);
            RecalculateResourcePositions();
        }

        private void RecalculateResourcePositions()
        {
            if (VisualizationMethod == ResourceVisualizationMethod.Percentage) return;
            for (int i = 0; i < _displayedResources.Count; ++i)
            {
                ResourceStacker.StackObject(_displayedResources[i], i, _displayedResources[i].IsMoving);
            }
        }
    }
}