using System;
using Kuantech.Core.UI;
using TMPro;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    /// <summary>
    /// A UI element to show collectible card
    /// </summary>
    public class CollectiblePreviewCard : UIElement
    {
        [Header("Visuals")] [SerializeField] private TMP_Text Name;
        public UIElementVisualStateHandler VisualStateHandler;
        private CollectibleDataAsset _currentCollectibleDataAsset;
        
        public void Initialize(CollectibleDataAsset dataAsset)
        {
            _currentCollectibleDataAsset = dataAsset;
        }
    }
}