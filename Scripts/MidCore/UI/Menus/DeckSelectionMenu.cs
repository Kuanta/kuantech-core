using System.Collections.Generic;
using Kuantech.Core.UI;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    public class DeckSelectionMenu : UIMenu
    {
        public int DeckIndex = 0;
        public CollectiblePreviewCard CollectiblePreviewCardPrefab;
        public List<CollectiblePreviewCard> DeckCards;
        public bool CanEquipCards = true;

        [Header("Comps")] 
        [SerializeField] private Button CancelEquipButton;
        
        [Header("Panels")] 
        [SerializeField] private CollectibleInfoPanel CollectibleInfoPanel;
        [SerializeField] private List<CollectiblesShowcasePanel> CollectiblesShowcasePanels;
        
        [Header("Regions")] 
        [SerializeField] private RectTransform EquippedCardsPanel;
        [SerializeField] private RectTransform AllCollectiblesPanel;
        
        //Runtime
        private CollectiblePreviewCard CurrentlySelectedCard; //last selected card
        private CollectiblePreviewCard CardToEquip; //card that will be equipped when the player clicks on a deck card
        
        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            DeckCards = new List<CollectiblePreviewCard>();

            foreach (var showcasePanels in CollectiblesShowcasePanels)
            {
                showcasePanels.SetCollectibles(this, CollectiblePreviewCardPrefab);
            }
            
            List<CollectableAsset> collectibleDataAssets = ProgressionManager.GetCollectibles();
            
            // //Create preview cards for all collectibles
            // foreach(var dataAsset in collectibleDataAssets)
            // {
            //     if(dataAsset.DeckIndex != DeckIndex) continue;
            //     CollectiblePreviewCard card = Instantiate(CollectiblePreviewCardPrefab, AllCollectiblesPanel);
            //     card.Initialize(this, false);
            //     card.SetCollectableAsset(dataAsset);
            //     CollectiblePreviewCards[dataAsset.GetId()] = card;
            // }
            
            //Equipped
            int deckSize = DeckBuildingManager.GetDeckSize(DeckIndex);
            List<ProgressibleData> currentDeck = DeckBuildingManager.GetCurrentDeck(DeckIndex);
            
            for(int i=0;i < deckSize; ++i)
            {
                CollectiblePreviewCard card = Instantiate(CollectiblePreviewCardPrefab, EquippedCardsPanel);
                card.Initialize(this, true);
                DeckCards.Add(card);
                card.transform.SetParent(EquippedCardsPanel);

                CollectableAsset dataAsset = null;
                if (currentDeck.IsValidIndex(i) && currentDeck[i] != null)
                {
                    dataAsset = ProgressionManager.GetCollectibleById(currentDeck[i].Id);
                }
                card.SetCollectableAsset(dataAsset);
            }
            
            CancelEquipButton.onClick.AddListener(ClearCardToEquip);
            CancelEquipButton.gameObject.SetActive(false);

            CollectibleInfoPanel.ParentDeckSelectionMenu = this;
        }
        
        public override void Open()
        {
            base.Open();
            CardToEquip = null;
            UpdateCards();
        }
        
        public void UpdateCards()
        {
            foreach (var panel in CollectiblesShowcasePanels)
            {
                panel.UpdateCards();
            }
            
            //Equipped
            int deckSize = DeckBuildingManager.GetDeckSize(DeckIndex);
            List<ProgressibleData> currentDeck = DeckBuildingManager.GetCurrentDeck(DeckIndex);
            
            for(int i=0;i < deckSize; ++i)
            {
                CollectiblePreviewCard card = DeckCards[i];

                CollectableAsset dataAsset = null;
                if (currentDeck.IsValidIndex(i) && currentDeck[i] != null)
                {
                    dataAsset = ProgressionManager.GetCollectibleById(currentDeck[i].Id);
                }
                if (card.CollectibleDataAsset != dataAsset)
                {
                    card.SetCollectableAsset(dataAsset);
                }
                else
                {
                    card.UpdatePreviewCard();
                }
            }
            
            
            //Sort collectibles
            AllCollectiblesPanel.transform.SortChildren(SortCards);
        }

        private int SortCards(Transform a, Transform b)
        {
            var cardA = a.gameObject.GetComponent<CollectiblePreviewCard>();
            var cardB = b.gameObject.GetComponent<CollectiblePreviewCard>();
            if (cardA == null) return 1;
            if (cardB == null) return -1;
            
            bool aUnlocked = ProgressionManager.IsProgressibleUnlocked(cardA.CollectibleDataAsset);
            bool bUnlocked = ProgressionManager.IsProgressibleUnlocked(cardB.CollectibleDataAsset);
            if(aUnlocked && !bUnlocked)
            {
                return -1; // A is unlocked, B is locked
            }

            if (bUnlocked && !aUnlocked)
            {
                return 1;
            }
            
            //Both locked, compare their required levels
            if (!aUnlocked && !bUnlocked)
            {
                if (cardA.CollectibleDataAsset.RequiredLevel > cardB.CollectibleDataAsset.RequiredLevel)
                {
                    return 1;
                }else if (cardA.CollectibleDataAsset.RequiredLevel < cardB.CollectibleDataAsset.RequiredLevel)
                {
                    return -1;
                }
            }
            
            //Compare their names
            return string.Compare(cardA.CollectibleDataAsset.GetName(), cardB.CollectibleDataAsset.GetName(), System.StringComparison.Ordinal);
        }
        public void OnPreviewCardClicked(CollectiblePreviewCard card)
        {
            if (!CanEquipCards)
            {
                if (card.CollectibleDataAsset == null ||
                    !ProgressionManager.IsProgressibleUnlocked(card.CollectibleDataAsset))
                {
                    return;
                }
                OpenCollectibleInfoPanel(card.CollectibleDataAsset);
                return;
            }
            
            //Is unlocked
            if (!card.IsDeckCard && (card.CollectibleDataAsset == null ||
                !ProgressionManager.IsProgressibleUnlocked(card.CollectibleDataAsset)))
            {
                ClearCardToEquip();
                return;
            }
            
            if (CurrentlySelectedCard != card)
            {
                DeselectCard();
            }
            
            if (CardToEquip == null)
            {
                if (card.IsDeckCard)
                {
                    OpenCollectibleInfoPanel(card.CollectibleDataAsset);
                }
                else
                {
                    SelectCard(card);
                }
            }
            else
            {
                if (card.IsDeckCard)
                {
                    //Swap
                    if(card.CollectibleDataAsset != null)
                    {
                        DeckBuildingManager.UnequipCollectible(card.CollectibleDataAsset);
                    }
                    DeckBuildingManager.EquipCollectible(CardToEquip.CollectibleDataAsset);
                    card.SetCollectible(CardToEquip.CollectibleDataAsset);
                }
                ClearCardToEquip();
                
            }
        }

        private void SelectCard(CollectiblePreviewCard card)
        {
            if(CurrentlySelectedCard != null)
            {
                CurrentlySelectedCard.ToggleSelected(false);
            }

            CurrentlySelectedCard = card;
            card.ToggleSelected(true);
        }

        private void DeselectCard()
        {
            if (CurrentlySelectedCard != null)
            {
                CurrentlySelectedCard.ToggleSelected(false);
            }

            CurrentlySelectedCard = null;
        }

        public void SetCardToEquip(CollectiblePreviewCard card)
        {
            if (CardToEquip == card) return;
            ClearCardToEquip();
            CardToEquip = card;
            CancelEquipButton.gameObject.SetActive(true);
            foreach (var deckCard in DeckCards)
            {
                if(deckCard == null) continue;
                deckCard.ToggleClickMeIndicator(true);
            }
        }

        public void ClearCardToEquip()
        {
            if(CardToEquip != null)
            {
                CardToEquip.ToggleSelected(false);
            }
            CancelEquipButton.gameObject.SetActive(false);
            CardToEquip = null;
            foreach (var deckCard in DeckCards)
            {
                if(deckCard == null) continue;
                deckCard.ToggleClickMeIndicator(false);
            }
        }
        
        public void OpenCollectibleInfoPanel(ProgressableDataAsset dataAsset)
        {
            if (CollectibleInfoPanel == null) return;
            DeselectCard();
            if (dataAsset is CollectableAsset deckCollectableAsset)
            {
                CollectibleInfoPanel.Open();
                CollectibleInfoPanel.UpdateInfoPanel(deckCollectableAsset);
            }
          
        }
    }
}