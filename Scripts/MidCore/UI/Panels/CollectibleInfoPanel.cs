using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.UI;
using Kuantech.Rpg;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [SerializeField] private Button EquipButton;
        [SerializeField] private Button UnequipButton;
        
        public List<AttributeIndicator> AttributeIndicators;
        private Dictionary<string, AttributeIndicator> _attributeIndicatorsById = new Dictionary<string, AttributeIndicator>();
        
        [NonSerialized] public CollectableAsset CurrentDataAsset;
        [NonSerialized] public DeckSelectionMenu ParentDeckSelectionMenu;

        // True for the one frame the panel opens on, so the same tap/click that opened it doesn't also
        // land on this frame's outside-tap check in LateUpdate and instantly close it again.
        private bool _suppressClose;

        public override void Open()
        {
            base.Open();
            _suppressClose = true;
        }

        // Closes on a tap/click that lands outside the panel — same pattern as PlayerInventoryPanel's
        // item-details close-on-outside-tap.
        private void LateUpdate()
        {
            if (_suppressClose) { _suppressClose = false; return; }
            if (!IsVisible()) return;
            if (!Input.GetMouseButtonUp(0)) return;

            var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var hit in results)
            {
                if (hit.gameObject.transform.IsChildOf(transform)) return; // tap landed on the panel itself
            }

            Close();
        }

        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            if(UpgradeButton != null) UpgradeButton.OnUpgradePurchased += OnUpgradePurchased;
            if(EquipButton != null)
            {
                EquipButton.onClick.AddListener(() =>
                {
                    DeckBuildingManager.EquipCollectible(CurrentDataAsset);
                    Close();
                });
            }
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
            bool isUnlocked = ProgressionManager.IsProgressibleUnlocked(dataAsset);
            if(UnequipButton != null) UnequipButton.gameObject.SetActive(isEquipped);
            if(EquipButton != null) EquipButton.gameObject.SetActive(isUnlocked && !isEquipped);
        }

        public virtual void UpdateStats(CollectableAsset collectableAsset)
        {
            if (_attributeIndicatorsById.IsNullOrEmpty())
            {
                foreach (var attributeIndicator in AttributeIndicators)
                {
                    if(attributeIndicator == null) continue;
                    _attributeIndicatorsById[attributeIndicator.AttributeAsset.GetId()] = attributeIndicator;
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
                    if(definition == null) continue;
                    //indicator.SetAttribute(definition, collectableLevel);
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