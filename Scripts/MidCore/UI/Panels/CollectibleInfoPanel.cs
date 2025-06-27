using System.Collections.Generic;
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

            foreach (var attribute in actorBlueprint.AttributeDefinitions)
            {
                if (_attributeIndicatorsById.ContainsKey(attribute.AttributeAsset.Id))
                {
                    _attributeIndicatorsById[attribute.AttributeAsset.Id].SetAttribute(attribute, collectableLevel);
                }
                    
            }

        }
    }
}