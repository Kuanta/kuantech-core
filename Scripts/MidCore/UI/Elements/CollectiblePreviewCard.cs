using Kuantech.Core.UI;
using Kuantech.Rpg.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    /// <summary>
    /// A UI element to show collectible card
    /// </summary>
    public class CollectiblePreviewCard : UIElement, IUIButtonAction
    {
        [Header("Visuals")] 
        [SerializeField] private TMP_Text Name;

        [SerializeField] private LevelableFloatIndicator LevelableFloatIndicator;
        [SerializeField] private Image CollectibleIcon;
        
        [SerializeField] private UnlockableUIElementVisualHandler VisualStateHandler;
        public ProgressableDataAsset CollectibleDataAsset;
        
        public void Initialize(ProgressableDataAsset dataAsset)
        {
            CollectibleDataAsset = dataAsset;
            if(CollectibleIcon != null) CollectibleIcon.sprite = dataAsset.Icon;
            VisualStateHandler.SetVisuals();
        }
        
        /// <summary>
        /// Updates the visuals of the card based on the current state of the collectible
        /// </summary>
        public void UpdatePreviewCard()
        {
            Initialize();
            if (CollectibleDataAsset == null) return;
            bool isUnlocked = ProgressionManager.IsProgressibleUnlocked(CollectibleDataAsset);
            var state = isUnlocked ? UnlockableStates.Unlocked : UnlockableStates.Locked;
            VisualStateHandler.SetVisual(state);
            
            var data = ProgressionManager.GetProgressibleData(CollectibleDataAsset);
            if (data == null) return;
            if(LevelableFloatIndicator != null) LevelableFloatIndicator.UpdateValue(data.GetRank());
            if(Name != null) Name.text = CollectibleDataAsset.Name;
        }

        public void OnClick()
        {
            Debug.Log("Clicked on collectible card: " + CollectibleDataAsset.Name);
        }
    }
}