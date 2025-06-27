using System.Collections.Generic;
using Kuantech.Core.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class DeckSelectionMenu : UIMenu
    {
        public CollectiblePreviewCard CollectiblePreviewCardPrefab;
        public List<CollectiblePreviewCard> DeckCards;
        
        //All cards on the collectibles menu
        public Dictionary<string, CollectiblePreviewCard> CollectiblePreviewCards = new Dictionary<string, CollectiblePreviewCard>();

        [Header("Panels")] 
        [SerializeField] private CollectibleInfoPanel CollectibleInfoPanel;
        
        [Header("Regions")] 
        [SerializeField] private RectTransform EquippedCardsPanel;
        [SerializeField] private RectTransform AllCollectiblesPanel;
        
        public override void Initialize()
        {
            base.Initialize();
            DeckCards = new List<CollectiblePreviewCard>();
            
            List<DeckCollectableAsset> collectibleDataAssets = DeckBuildingManager.GetCollectibles();
            
            //Create preview cards for all collectibles
            foreach(var dataAsset in collectibleDataAssets)
            {
                CollectiblePreviewCard card = Instantiate(CollectiblePreviewCardPrefab, AllCollectiblesPanel);
                card.Initialize(dataAsset);
                card.OnDeckCardClicked += OnPreviewCardClicked;
                CollectiblePreviewCards[dataAsset.GetId()] = card;
            }
            
            //Equipped
            int deckSize = DeckBuildingManager.GetDeckSize();
            List<ProgressibleData> currentDeck = DeckBuildingManager.GetCurrentDeck();
            
            for(int i=0;i < deckSize; ++i)
            {
                ProgressibleData data = DeckBuildingManager.GetCurrentDeck()[i];
                if (data == null) continue;
                
                CollectiblePreviewCard card = Instantiate(CollectiblePreviewCardPrefab, EquippedCardsPanel);
                card.OnDeckCardClicked += OnPreviewCardClicked;
                card.IsDeckCard = true;

                DeckCollectableAsset dataAsset = null;
                if (currentDeck.IsValidIndex(i))
                {
                    
                    dataAsset = DeckBuildingManager.GetProgressibleDataAssetById(currentDeck[i].Id);

                }
                card.Initialize(dataAsset);
                DeckCards.Add(card);
                card.transform.SetParent(EquippedCardsPanel);
            }
        }
        
        public override void Open()
        {
            base.Open();
            UpdateCards();
        }
        
        private void UpdateCards()
        {
            foreach (var card in CollectiblePreviewCards.Values)
            {
                card.UpdatePreviewCard();
            }
        }

        private void OnPreviewCardClicked(CollectiblePreviewCard card)
        {
            Debug.Log("Clicked on collectible card: " + card.CollectibleDataAsset.Name);
            UpdateCollectibleInfoPanel(card.CollectibleDataAsset);
        }

        private void UpdateCollectibleInfoPanel(ProgressableDataAsset dataAsset)
        {
            if (dataAsset is DeckCollectableAsset deckCollectableAsset)
            {
                CollectibleInfoPanel.Open();
                CollectibleInfoPanel.UpdateInfoPanel(deckCollectableAsset);
            }
          
        }
    }
}