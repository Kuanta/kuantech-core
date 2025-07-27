using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.UI;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

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
        [SerializeField] private RectTransform ContentParent;
        [SerializeField] private RectTransform ButtonsParent;
        [SerializeField] private Vector2 ResumePositionOffset = new Vector2(0, 0);
 
        private List<UpgradeTreeButton> _upgradeButtons;
        private Dictionary<(ProgressableDataAsset, int), UpgradeTreeButton> _upgradeButtonsMap;
        
        //Runtime
        [NonSerialized] public UpgradeTreeButton SelectedButton;

        public override void Open()
        {
            base.Open();

            int unlockedIndex = 0;
            foreach (var button in _upgradeButtons)
            {
                if (button.CurrentState == UnlockableStates.Purchasable)
                {
                    //Move center to this
                    Vector2 anchoredPos = new Vector2(0, -button.GetComponent<RectTransform>().anchoredPosition.y) +
                                          ResumePositionOffset;
                    anchoredPos.y = Mathf.Min(anchoredPos.y, 0);
                    ContentParent.anchoredPosition = anchoredPos;
                    break;
                }
            }

        }
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
                button.ParentMenu = this;
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

        public void SelectButton(UpgradeTreeButton button)
        {
            if (button == null) return;
            if (SelectedButton != null) SelectedButton.OnDeselected();
            SelectedButton = button;
            SelectedButton.OnSelected();
        }

        public void DeselectButton()
        {
            if (SelectedButton != null) SelectedButton.OnDeselected();
            SelectedButton = null;
        }

        #region Editor Methods

        [Header("Menu Building")] 
        [SerializeField] private List<ProgressableDataAsset> LoopingProgressables;
        [SerializeField] private int MaxRank = 20;
        [SerializeField] private UpgradeTreeButton UpgradeTreeButtonPrefab;
        [SerializeField] private float BottomPadding = 500f;
        [SerializeField] private float Spacing = 500.0f;
        [SerializeField] private Vector2 Axis = new Vector2(1, 0);
        [SerializeField] private Vector2 AnchorsMax = new Vector2(1, 1);
        [SerializeField] private Vector2 AnchorsMin = new Vector2(0, 0);
        
        /// <summary>
        /// Creates a dependency tree of looping
        /// </summary>
        /// <param name="progressables"></param>
        [Button("Build Buttons")]
        public void BuildButtons()
        {
            ButtonsParent.DestroyAllChildren();
            int counter = 0;
            UpgradeTreeButton previousButton = null;
            
            for(int i=0; i < MaxRank; ++i)
            {
                foreach (var progressable in LoopingProgressables)
                {
                    if (progressable == null) continue;
                    UpgradeTreeButton button = Helpers.InstantiatePrefab(UpgradeTreeButtonPrefab.gameObject).GetComponent<UpgradeTreeButton>();
                    button.Rank = i;
                    button.UpgradeDataAsset = progressable;
                    button.transform.SetParent(ButtonsParent);
                    button.transform.SetSiblingIndex(0); //Always on top
                    RectTransform rectTransform = button.GetComponent<RectTransform>();
                    rectTransform.anchorMin = AnchorsMin;
                    rectTransform.anchorMax = AnchorsMax;
                    rectTransform.anchoredPosition = Axis * (BottomPadding + counter * Spacing);
                    counter++;
                    button.DependsOn = previousButton;
                    previousButton = button;
                }
            }
        }

        #endregion
    }
}