using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.UI;
using Kuantech.Rpg;
using Kuantech.Rpg.UI;
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
        [SerializeField] private LevelableFloatIndicator CollectibleLevelIndicator;
        [SerializeField] private UpgradeButton UpgradeButton;
        
        public List<AttributeIndicator> AttributeIndicators;
        private Dictionary<string, AttributeIndicator> _attributeIndicatorsById = new Dictionary<string, AttributeIndicator>();
        
        [NonSerialized] public DeckCollectableAsset CurrentDataAsset;
        [NonSerialized] public DeckSelectionMenu ParentDeckSelectionMenu;
        
        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            UpgradeButton.OnUpgradePurchased += OnUpgradePurchased;
        }
        
        public virtual void UpdateInfoPanel(DeckCollectableAsset dataAsset)
        {
            if(dataAsset == null) return;
            CurrentDataAsset = dataAsset;
            if (Name != null) Name.text = dataAsset.GetName();
            if(Description != null) Description.text = dataAsset.GetDescription();
            if (Icon != null)
            {
                Icon.sprite = dataAsset.GetIcon();
            }
            
            UpgradeButton.SetProgressable(dataAsset);
            
            UpdateStats(dataAsset);

            if (CollectibleLevelIndicator != null)
            {
                CollectibleLevelIndicator.UpdateValue(ProgressionManager.GetCurrentRank(dataAsset));
            }
        }

        public virtual void UpdateStats(DeckCollectableAsset deckCollectableAsset)
        {
            if (_attributeIndicatorsById.IsNullOrEmpty())
            {
                foreach (var attributeIndicator in AttributeIndicators)
                {
                    if(attributeIndicator == null) continue;
                    _attributeIndicatorsById[attributeIndicator.AttributeAsset.Id] = attributeIndicator;
                }
            }
            ActorBlueprint actorBlueprint = deckCollectableAsset.ActorBlueprint;
            int collectableLevel = ProgressionManager.GetCurrentRank(deckCollectableAsset);

            StatsSetterComponent statsSetter =
                actorBlueprint.GetActorBlueprintComponent<StatsSetterComponent>();
            if (statsSetter == null) return;

            foreach (var indicator in AttributeIndicators)
            {
                AttributeDefinition definition = statsSetter.GetAttributeDefinition(indicator.AttributeAsset);
                indicator.SetAttribute(definition, collectableLevel);
            }
        }

        private void OnUpgradePurchased()
        {
            //Do effects
            UpdateStats(CurrentDataAsset);

            if (CollectibleLevelIndicator != null)
            {
                CollectibleLevelIndicator.UpdateValue(ProgressionManager.GetCurrentRank(CurrentDataAsset));
            }
            
            ParentDeckSelectionMenu.UpdateCards();
        }
    }
}