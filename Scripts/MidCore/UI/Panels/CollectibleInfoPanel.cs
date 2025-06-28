using System.Collections.Generic;
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
        public virtual void UpdateInfoPanel(DeckCollectableAsset dataAsset)
        {
            if(dataAsset == null) return;
            if (Name != null) Name.text = dataAsset.Name;
            if(Description != null) Description.text = dataAsset.Description;
            if (Icon != null)
            {
                Icon.sprite = dataAsset.Icon;
            }
            
            UpgradeButton.SetProgressable(dataAsset);
            
            UpdateStats(dataAsset);
        }

        public virtual void UpdateStats(DeckCollectableAsset deckCollectableAsset)
        {
            if (_attributeIndicatorsById.IsNullOrEmpty())
            {
                foreach (var attributeIndicator in AttributeIndicators)
                {
                    _attributeIndicatorsById[attributeIndicator.AttributeAsset.Id] = attributeIndicator;
                }
            }
            ActorBlueprint actorBlueprint = deckCollectableAsset.ActorBlueprint;
            int collectableLevel = ProgressionManager.GetCurrentRank(deckCollectableAsset);

            StatsLoaderActorBlueprintComponent statsLoader =
                actorBlueprint.GetActorBlueprintComponent<StatsLoaderActorBlueprintComponent>();
            if (statsLoader == null) return;

            foreach (var indicator in AttributeIndicators)
            {
                AttributeDefinition definition = statsLoader.GetAttributeDefinition(actorBlueprint.GetId(), indicator.AttributeAsset);
                indicator.SetAttribute(definition, collectableLevel);
            }
        }
    }
}