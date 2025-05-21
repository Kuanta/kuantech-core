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

            ProgressionManager pm = ProgressionManager.GetContext<ProgressionManager>();
            if (pm == null) return;
            List<ProgressableDataAsset> collectibleDataAssets = pm.Collectibles;

            foreach (var dataAsset in collectibleDataAssets)
            {
                AddCollectiblePreviewCard(dataAsset);
            }
            
            pm.OnUpgradeUnlocked += OnProgressibleUnlocked;
            pm.OnUpgradeRankSet += OnProgressibleRankSet;
            pm.OnSubUpgradeRankSet += OnSubUpgradeRankSet;
        }

        public override void Show()
        {
            base.Show();
            UpdatePreviewCards();
        }
        
        /// <summary>
        /// Updates the preview cards based on the current state of the collectibles
        /// </summary>
        private void UpdatePreviewCards()
        {
            foreach (var pair in CollectiblePreviewCards)
            {
                bool isUnlocked = ProgressionManager.IsProgressibleUnlocked(pair.Value.CollectibleDataAsset);
                if(isUnlocked)
                {
                    pair.Value.transform.SetParent(UnlockedCards);
                }
                else
                {
                    pair.Value.transform.SetParent(LockedCards);
                }
                pair.Value.UpdatePreviewCard();
            }
        }
        
        public void AddCollectiblePreviewCard(ProgressableDataAsset collectibleDataAsset)
        {
            if (CollectiblePreviewCards.ContainsKey(collectibleDataAsset.Id)) return;
            CollectiblePreviewCard collectiblePreviewCard = Instantiate(CollectiblePreviewCardPrefab, LockedCards);
            collectiblePreviewCard.Initialize(collectibleDataAsset);

            if (ProgressionManager.IsProgressibleUnlocked(collectibleDataAsset))
            {
                collectiblePreviewCard.transform.SetParent(UnlockedCards);
            }
            else
            {
                collectiblePreviewCard.transform.SetParent(LockedCards);
            }
            CollectiblePreviewCards.Add(collectibleDataAsset.Id, collectiblePreviewCard);
        }

        public CollectiblePreviewCard GetCollectiblePreviewCard(ProgressibleData data)
        {
            if (CollectiblePreviewCards.ContainsKey(data.Id)) return CollectiblePreviewCards[data.Id];
            return null;
        }
        private void OnProgressibleUnlocked(ProgressibleData data)
        {
            //Get preview card
            CollectiblePreviewCard card = GetCollectiblePreviewCard(data);
            if (card == null) return;
            card.transform.SetParent(UnlockedCards);
        }
        
        private void OnProgressibleRankSet(ProgressibleData data)
        {
            CollectiblePreviewCard card = GetCollectiblePreviewCard(data);
            if (card == null) return;
            card.UpdatePreviewCard();
        }

        private void OnSubUpgradeRankSet(ProgressibleData data)
        {
            CollectiblePreviewCard card = GetCollectiblePreviewCard(data);
            if (card == null) return;
            card.UpdatePreviewCard();
        }
    }
}