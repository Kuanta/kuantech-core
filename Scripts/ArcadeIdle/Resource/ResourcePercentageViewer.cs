using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    /// <summary>
    /// Resource visualiser for percentage view. 
    /// </summary>
    
    [Serializable]
    public struct PercentageViewerElement
    {
        public GameObject Visual;
        public int MinAmount;
    }    
    public class ResourcePercentageViewer : MonoBehaviour {
        public ResourceInventory SourceInventory;
        public List<PercentageViewerElement> ViewStages;
        public void UpdateView()
        {
            int amount = SourceInventory.GetCarriedResourcesCount();
            GameObject objectToView = null;
            foreach(var stage in ViewStages)
            {
                if(amount >= stage.MinAmount && objectToView == null)
                {
                    objectToView = stage.Visual;
                }
                stage.Visual.SetActive(false);
            }
            if(objectToView != null)
            {
                objectToView.SetActive(true);
            }
        }
    }
}