using System;
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
    public class CollectiblePreviewCard : UIElement, KtButton.IUIButtonAction
    {
        [Header("Visuals")] 
        [SerializeField] private TMP_Text Name;
        [SerializeField] private LevelableFloatIndicator LevelableFloatIndicator;
        [SerializeField] private Image CollectibleIcon;
        [SerializeField] private Image LockedCollectibleIcon;
        [SerializeField] private UnlockableUIElementVisualHandler VisualStateHandler;
        
        [Header("Indicators")]
        [SerializeField] private GameObject ClickMeIndicator;
        [SerializeField] private GameObject EquippedIndicator;
        [SerializeField] private LevelRequirementIndicator LevelRequirementIndicator;
        
        [Header("Selected Panel")]
        [SerializeField] private GameObject SelectedPanel;
        [SerializeField] private GameObject ContentsParent;

        [Header("Buttons")] 
        [SerializeField] private KtButton InfoButton;
        [SerializeField] private KtButton EquipButton;
        
        //Runtime
        [NonSerialized] public bool IsDeckCard; //If its a deck card, clicking on it will open the info panel
        public CollectableAsset CollectibleDataAsset;
        private DeckSelectionMenu _parentMenu;
        private bool _selected = false;

        public void Initialize(DeckSelectionMenu deckSelectionMenu, bool isDeckCard)
        {
            if (!Initialized) base.Initialize();
            IsDeckCard = isDeckCard;
            if (IsDeckCard)
            {
                EquipButton.gameObject.SetActive(false);
            }
            _parentMenu = deckSelectionMenu;
            if(InfoButton != null) InfoButton.onClick.AddListener(OnInfoButtonClicked);
            if(EquipButton != null) EquipButton.onClick.AddListener(OnEquipButtonClicked);
 
        }
        
        public void SetCollectableAsset(CollectableAsset dataAsset)
        {
            CollectibleDataAsset = dataAsset;
            VisualStateHandler.SetVisuals();
            SetCollectible(dataAsset);
            if(LevelRequirementIndicator != null)
            {
                LevelRequirementIndicator.SetProgressable(dataAsset, 0);
            }
            UpdatePreviewCard();
        }

        private void SetCollectible(CollectableAsset dataAsset)
        {
            CollectibleDataAsset = dataAsset;
            if(CollectibleDataAsset != null && CollectibleIcon != null) CollectibleIcon.sprite = dataAsset.GetIcon();
            if (CollectibleDataAsset != null && LockedCollectibleIcon != null)
                LockedCollectibleIcon.sprite = dataAsset.GetIcon();
        }
        
        /// <summary>
        /// Updates the visuals of the card based on the current state of the collectible
        /// </summary>
        public void UpdatePreviewCard()
        {
            if(!Initialized) Initialize();
            
            ContentsParent.gameObject.SetActive(CollectibleDataAsset != null);

            if (CollectibleDataAsset == null)
            {
                VisualStateHandler.SetVisual(UnlockableStates.Empty);
                return;
            }
            
            bool equipped = DeckBuildingManager.IsEquipped(CollectibleDataAsset);
            
            if(EquippedIndicator != null && !IsDeckCard)
            {
                EquippedIndicator.SetActive(equipped);
            }
    
            bool isUnlocked = ProgressionManager.IsProgressibleUnlocked(CollectibleDataAsset);
            if(ContentsParent != null) ContentsParent.SetActive(isUnlocked);
            
            var state = isUnlocked ? UnlockableStates.Unlocked : UnlockableStates.Locked;
            VisualStateHandler.SetVisual(state);
            
            var data = ProgressionManager.GetProgressibleData(CollectibleDataAsset);
            if (data == null)
            {
                return;
            }
            if(LevelableFloatIndicator != null) LevelableFloatIndicator.UpdateValue(data.GetRank());
            if(Name != null) Name.text = CollectibleDataAsset.GetName();
        }
        
        public virtual void OnClick()
        {
            if (_selected) return; //Already selected
            _parentMenu.OnPreviewCardClicked(this);
        }

        private void OnInfoButtonClicked()
        {
            _parentMenu.OpenCollectibleInfoPanel(CollectibleDataAsset);
        }

        private void OnEquipButtonClicked()
        {
            _parentMenu.SetCardToEquip(this);
        }

        private Canvas _canvas;
        private GraphicRaycaster _graphicRaycaster;
        public void ToggleSelected(bool toggle)
        {
            if (_selected && toggle) return;
            //todo(animation): Do an animation here
            SelectedPanel.SetActive(toggle);
            if (toggle)
            {
                _canvas = gameObject.AddComponent<Canvas>();
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = 100; // Ensure this card is on top;
                _graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
              
            }
            else
            {
                if(_graphicRaycaster != null) Destroy(_graphicRaycaster);
                _graphicRaycaster = null;
                if(_canvas != null) Destroy(_canvas);
                _canvas = null;
            }
            _selected = toggle;

        }
        
        public void ToggleClickMeIndicator(bool show)
        {
            if (!IsDeckCard) show = false;
            if (ClickMeIndicator != null) ClickMeIndicator.SetActive(show);
        }
        
    }
}