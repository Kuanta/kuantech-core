using System.Collections.Generic;
using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    /// <summary>
    /// A ui element that shows all collectibles for given deck id
    /// </summary>
    public class CollectiblesShowcasePanel : UIElement
    {
        public int DeckIndex = 0;

        [SerializeField] private Transform Content;
        //All cards on the collectibles menu
        public Dictionary<string, CollectiblePreviewCard> CollectiblePreviewCards = new Dictionary<string, CollectiblePreviewCard>();
       
        
        public void SetCollectibles(DeckSelectionMenu deckSelectionMenu, CollectiblePreviewCard collectiblePreviewCardPrefab)
        {
            List<CollectableAsset> collectibleDataAssets = ProgressionManager.GetCollectiblesById(DeckIndex);
            //Create preview cards for all collectibles
            foreach(var dataAsset in collectibleDataAssets)
            {
                if(dataAsset.DeckIndex != DeckIndex) continue;
                CollectiblePreviewCard card = Instantiate(collectiblePreviewCardPrefab, Content);
                card.Initialize(deckSelectionMenu, false);
                card.SetCollectableAsset(dataAsset);
                CollectiblePreviewCards[dataAsset.GetId()] = card;
            }
        }

        public void UpdateCards()
        {
            foreach (var card in CollectiblePreviewCards.Values)
            {
                card.UpdatePreviewCard();
                card.ToggleClickMeIndicator(false);
            }
        }

        public void DeselectCards()
        {
            foreach (var card in CollectiblePreviewCards.Values)
            {
                card.ToggleSelected(false);
            }
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


    }
}