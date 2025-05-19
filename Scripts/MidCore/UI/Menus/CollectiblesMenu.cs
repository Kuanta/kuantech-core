using System.Collections.Generic;
using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class CollectiblesMenu : UIMenu
    {
        public CollectiblePreviewCard CollectiblePreviewCardPrefab;
        public Dictionary<string, CollectiblePreviewCard> CollectiblePreviewCards = new Dictionary<string, CollectiblePreviewCard>();

        [Header("Regions")] 
        [SerializeField] private RectTransform LockedCards;
        [SerializeField] private RectTransform UnlockedCards;
        
        public override void Initialize()
        {
            base.Initialize();

            List<CollectibleDataAsset> collectibleDataAssets;

            CollectiblesHandler ch = ProgressionManager.GetCollectiblesHandler();
            if (ch == null)
            {
                Debug.LogWarning("CollectiblesHandler not found in the ProgressionManager.");
                return;
            }
            collectibleDataAssets = ch.AllCollectibles;
        }
        
        public void AddCollectibleCard(CollectibleDataAsset collectibleDataAsset)
        {
            if (CollectiblePreviewCards.ContainsKey(collectibleDataAsset.CollectibleId)) return;
            CollectiblePreviewCard collectiblePreviewCard = Instantiate(CollectiblePreviewCardPrefab, LockedCards);
            CollectiblePreviewCards.Add(collectibleDataAsset.CollectibleId, collectiblePreviewCard);
            collectiblePreviewCard.Initialize(collectibleDataAsset);
        }
        
    }
}