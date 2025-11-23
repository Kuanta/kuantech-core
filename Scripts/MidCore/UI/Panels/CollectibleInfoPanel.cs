using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.UI;
using Kuantech.Rpg;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    public class CollectibleInfoPanel : UIMenu
    {
        [Header("Components")] 
        [SerializeField] private TMP_Text Name;
        [SerializeField] private TMP_Text Description;
        [SerializeField] private Image Icon;
        [SerializeField] private CollectableRankIndicator CollectibleLevelIndicator;
        [SerializeField] private UpgradeButton UpgradeButton;
        [SerializeField] private Button UnequipButton;
        
        public List<AttributeIndicator> AttributeIndicators;
        private Dictionary<string, AttributeIndicator> _attributeIndicatorsById = new Dictionary<string, AttributeIndicator>();
        
        [NonSerialized] public CollectableAsset CurrentDataAsset;
        [NonSerialized] public DeckSelectionMenu ParentDeckSelectionMenu;
        
        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            if(UpgradeButton != null) UpgradeButton.OnUpgradePurchased += OnUpgradePurchased;
            if(UnequipButton != null)
            {
                UnequipButton.onClick.AddListener(() =>
                {
                    DeckBuildingManager.UnequipCollectible(CurrentDataAsset);
                    Close();
                });
            }
        }
        
        public virtual void UpdateInfoPanel(CollectableAsset dataAsset)
        {
            if(dataAsset == null) return;
            CurrentDataAsset = dataAsset;
            if (Name != null) Name.text = dataAsset.GetName();
            if(Description != null) Description.text = dataAsset.GetDescription();
            if (Icon != null)
            {
                Icon.sprite = dataAsset.GetIcon();
            }
            
            if(UpgradeButton != null) UpgradeButton.SetProgressable(dataAsset);
            
            UpdateStats(dataAsset);

            if (CollectibleLevelIndicator != null)
            {
                CollectibleLevelIndicator.SetCollectableRank(dataAsset);
            }
            
            bool isEquipped = DeckBuildingManager.IsEquipped(dataAsset);
             if(UnequipButton != null) UnequipButton.gameObject.SetActive(isEquipped);
        }

        public virtual void UpdateStats(CollectableAsset collectableAsset)
        {
            if (_attributeIndicatorsById.IsNullOrEmpty())
            {
                foreach (var attributeIndicator in AttributeIndicators)
                {
                    if(attributeIndicator == null) continue;
                    _attributeIndicatorsById[attributeIndicator.AttributeAsset.Id] = attributeIndicator;
                }
            }
            ActorBlueprint actorBlueprint = collectableAsset.ActorBlueprint;
            
            int collectableLevel = collectableAsset.GetCollectableRank();

            if (actorBlueprint != null)
            {
                StatsSetterComponent statsSetter =
                    actorBlueprint.GetActorBlueprintComponent<StatsSetterComponent>();
                if (statsSetter == null) return;

                foreach (var indicator in AttributeIndicators)
                {
                    AttributeDefinition definition = statsSetter.GetAttributeDefinition(indicator.AttributeAsset);
                    indicator.SetAttribute(definition, collectableLevel);
                }
            }
        }

        private void OnUpgradePurchased()
        {
            //Do effects
            UpdateStats(CurrentDataAsset);

            if (CollectibleLevelIndicator != null)
            {
                CollectibleLevelIndicator.SetCollectableRank(CurrentDataAsset);
            }
            
            ParentDeckSelectionMenu.UpdateCards();
        }
    }
}