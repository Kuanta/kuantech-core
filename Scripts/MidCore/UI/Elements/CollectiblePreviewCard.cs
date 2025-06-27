using System;
using Kuantech.Core.UI;
using Kuantech.Rpg.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    /// <summary>
    /// A UI element to show collectible card
    /// </summary>
    public class CollectiblePreviewCard : UIElement, KtButton.IUIButtonAction
    {
        [Header("Visuals")] 
        [SerializeField] private TMP_Text Name;

        [SerializeField] private LevelableFloatIndicator LevelableFloatIndicator;
        [SerializeField] private Image CollectibleIcon;
        
        [SerializeField] private UnlockableUIElementVisualHandler VisualStateHandler;
        public DeckCollectableAsset CollectibleDataAsset;

        [SerializeField] private GameObject SelectedVisual;

        [Header("Buttons")] 
        [SerializeField] private KtButton UpgradeButton;

        [NonSerialized] public bool IsDeckCard; //If its a deck card, clicking on it will open the info panel

        public UnityAction<CollectiblePreviewCard> OnDeckCardClicked;
        
        public void Initialize(DeckCollectableAsset dataAsset)
        {
            CollectibleDataAsset = dataAsset;
            VisualStateHandler.SetVisuals();
            SetCollectible(dataAsset);
        }

        public void SetCollectible(DeckCollectableAsset dataAsset)
        {
            CollectibleDataAsset = dataAsset;
            if(CollectibleDataAsset != null && CollectibleIcon != null) CollectibleIcon.sprite = dataAsset.Icon;
        }
        
        /// <summary>
        /// Updates the visuals of the card based on the current state of the collectible
        /// </summary>
        public void UpdatePreviewCard()
        {
            Initialize();
            if (CollectibleDataAsset == null)
            {
                VisualStateHandler.SetVisual(UnlockableStates.Locked);
                return;
            }
            
            bool isUnlocked = ProgressionManager.IsProgressibleUnlocked(CollectibleDataAsset);
            var state = isUnlocked ? UnlockableStates.Unlocked : UnlockableStates.Locked;
            VisualStateHandler.SetVisual(state);
            
            var data = ProgressionManager.GetProgressibleData(CollectibleDataAsset);
            if (data == null) return;
            if(LevelableFloatIndicator != null) LevelableFloatIndicator.UpdateValue(data.GetRank());
            if(Name != null) Name.text = CollectibleDataAsset.Name;
        }

        public void ToggleSelected(bool selected)
        {
            SelectedVisual.SetActive(selected);
        }
        
        public virtual void OnClick()
        {
            if (IsDeckCard)
            {
                OnDeckCardClicked?.Invoke(this);
            }
        }
    }
}