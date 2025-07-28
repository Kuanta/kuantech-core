using System.Collections.Generic;
using Kuantech.Core.UI;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    public class DeckSelectionMenu : UIMenu
    {
        public CollectiblePreviewCard CollectiblePreviewCardPrefab;
        public List<CollectiblePreviewCard> DeckCards;
        
        //All cards on the collectibles menu
        public Dictionary<string, CollectiblePreviewCard> CollectiblePreviewCards = new Dictionary<string, CollectiblePreviewCard>();

        [Header("Comps")] 
        [SerializeField] private Button CancelEquipButton;
        
        [Header("Panels")] 
        [SerializeField] private CollectibleInfoPanel CollectibleInfoPanel;
        
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
            
            List<DeckCollectableAsset> collectibleDataAssets = DeckBuildingManager.GetCollectibles();
            
            //Create preview cards for all collectibles
            foreach(var dataAsset in collectibleDataAssets)
            {
                CollectiblePreviewCard card = Instantiate(CollectiblePreviewCardPrefab, AllCollectiblesPanel);
                card.Initialize(this, false);
                card.SetCollectableAsset(dataAsset);
                CollectiblePreviewCards[dataAsset.GetId()] = card;
            }
            
            //Equipped
            int deckSize = DeckBuildingManager.GetDeckSize();
            List<ProgressibleData> currentDeck = DeckBuildingManager.GetCurrentDeck();
            
            for(int i=0;i < deckSize; ++i)
            {
                CollectiblePreviewCard card = Instantiate(CollectiblePreviewCardPrefab, EquippedCardsPanel);
                card.Initialize(this, true);
                DeckCards.Add(card);
                card.transform.SetParent(EquippedCardsPanel);

                DeckCollectableAsset dataAsset = null;
                if (currentDeck.IsValidIndex(i) && currentDeck[i] != null)
                {
                    dataAsset = DeckBuildingManager.GetProgressibleDataAssetById(currentDeck[i].Id);
                }
                card.SetCollectableAsset(dataAsset);
            }
            
            CancelEquipButton.onClick.AddListener(ClearCardToEquip);
            CancelEquipButton.gameObject.SetActive(false);
        }

        public override void Open()
        {
            base.Open();
            CardToEquip = null;
            UpdateCards();
        }
        
        private void UpdateCards()
        {
            foreach (var card in CollectiblePreviewCards.Values)
            {
                card.UpdatePreviewCard();
                card.ToggleClickMeIndicator(false);
            }
            
            // //Update deck cards
            // foreach (var deckCard in DeckCards)
            // {
            //     deckCard.UpdatePreviewCard();
            // }
            //
            
            //Equipped
            int deckSize = DeckBuildingManager.GetDeckSize();
            List<ProgressibleData> currentDeck = DeckBuildingManager.GetCurrentDeck();
            
            for(int i=0;i < deckSize; ++i)
            {
                CollectiblePreviewCard card = DeckCards[i];

                DeckCollectableAsset dataAsset = null;
                if (currentDeck.IsValidIndex(i) && currentDeck[i] != null)
                {
                    dataAsset = DeckBuildingManager.GetProgressibleDataAssetById(currentDeck[i].Id);
                }
                if (card.CollectibleDataAsset != dataAsset)
                {
                    card.SetCollectableAsset(dataAsset);
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
            
            //Compare their names
            return string.Compare(cardA.CollectibleDataAsset.GetName(), cardB.CollectibleDataAsset.GetName(), System.StringComparison.Ordinal);
        }
        public void OnPreviewCardClicked(CollectiblePreviewCard card)
        {
            //Is unlocked
            if (card.IsDeckCard && (card.CollectibleDataAsset == null ||
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
            if (dataAsset is DeckCollectableAsset deckCollectableAsset)
            {
                CollectibleInfoPanel.Open();
                CollectibleInfoPanel.UpdateInfoPanel(deckCollectableAsset);
            }
          
        }
    }
}