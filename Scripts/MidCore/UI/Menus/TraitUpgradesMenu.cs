using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.UI;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Midcore.UI
{
    /// <summary>
    /// A tree like trait upgrades menu
    /// </summary>
    public class TraitUpgradesMenu : UIMenu
    {
        [Header("Settings")] 
        [Tooltip("If set to true, tries to connect dependency conenctors automatically")]
        [SerializeField] private bool AutoConenctConnections = false;
        
        [Header("Components")] [SerializeField]
        private ContentLazyLoader ContentLazyLoader;
        
        [Header("Sections")] 
        [SerializeField] private RectTransform ButtonsParent;
        
        [Header("Button Prefabs")] [SerializeField]
        private UpgradeTreeButton upgradeTreeButtonPrefab;
        private List<UpgradeTreeButton> _upgradeButtons;
        private Dictionary<(ProgressableDataAsset, int), UpgradeTreeButton> _upgradeButtonsMap;
        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            _upgradeButtons = GetComponentsInChildren<UpgradeTreeButton>(true).ToList();
            if (_upgradeButtons.IsNullOrEmpty()) return;
            _upgradeButtonsMap = new Dictionary<(ProgressableDataAsset, int), UpgradeTreeButton>();
            //Fill the dictionary
            foreach(var button in _upgradeButtons)
            {
                if (button == null) continue;
                button.Initialize();
                if (button.UpgradeDataAsset == null) continue;
                if (button.Rank < 0) continue;
                
                //Add to dictionary
                _upgradeButtonsMap.Add((button.UpgradeDataAsset, button.Rank), button);
            }

            if (AutoConenctConnections)
            {
                foreach (var childButton in _upgradeButtons)
                {
                    
                    ProgressableDependencyEntry entry = ProgressionManager.GetProgressableDependencyEntry(childButton.UpgradeDataAsset, childButton.Rank);
                    if(entry == null) continue;
                    foreach (var condition in entry.UnlockConditions)
                    {
                        var key = (condition.DependingAsset, condition.DependingProgressionRank);
                        if (_upgradeButtonsMap.ContainsKey(key))
                        {
                            UpgradeTreeButton btnToConnect = _upgradeButtonsMap[key];
                            childButton.ConnectToButton(btnToConnect);
                        }
                    }
                    
                }
            }
            List<RectTransform> buttonRectTransforms = new List<RectTransform>();
            foreach (var button in _upgradeButtons)
            {
                buttonRectTransforms.Add(button.GetComponent<RectTransform>());
            }
            if (ContentLazyLoader != null)
            {
                ContentLazyLoader.SetTrackedItems(buttonRectTransforms);
            }
            
            //Subscribe to events
            ProgressionManager.GetContext<ProgressionManager>().OnUpgradeRankSet += UpdateButtons;
            ProgressionManager.GetContext<ProgressionManager>().OnUpgradeUnlocked += UpdateButtons;
        }
        
        private void UpdateButtons(ProgressibleData data)
        {
            //todo: Optimize by updating only corresbonding buttons
            if (_upgradeButtons.IsNullOrEmpty()) return;
            foreach (var button in _upgradeButtons)
            {
                if(button == null) continue;
                button.UpdateVisualState();
            }
        }
    }
}