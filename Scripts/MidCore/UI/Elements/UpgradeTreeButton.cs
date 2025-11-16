using System;
using System.Collections.Generic;
using Kuantech.Core.FX;
using Kuantech.Core.HyperCasual;
using Kuantech.Core.UI;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    /// <summary>
    /// Represents a button in trait upgrade tree
    /// </summary>
    public class UpgradeTreeButton : UIElement, KtButton.IUIButtonAction
    {

        [NonSerialized] public UnlockableStates CurrentState;
        
        [Header("Progression Asset")] 
        public ProgressableDataAsset UpgradeDataAsset;
        public int Rank;

        [Header("Purchase Button")] [SerializeField]
        private UpgradeButtonPanel UpgradeButtonPanel;
        
        [Header("Common Visuals")] 
        [SerializeField] private Image Icon;
        [SerializeField] private TMP_Text Name;
        [SerializeField] private PricePanel PriceTag;
        
        [Header("Connection")] 
        [SerializeField] private ConnectorUILine LineRenderer;
        public UpgradeTreeButton DependsOn;
        [SerializeField] private List<RectTransform> ConnectorControlPoints;
        
        [Header("Visual States")]
        [SerializeField] private GameObject LockedState;
        [SerializeField] private GameObject PurchasableState;
        [SerializeField] private GameObject UnlockedState;

        [Header("Lines")] 
        [SerializeField] private GameObject LockedLine;
        [SerializeField] private GameObject PurchasableLine;
        [SerializeField] private GameObject UnlockedLine;

        [Header("Effects & Anims")] [SerializeField]
        private Animator Animator;
        [KTTag("AudioTag")] [SerializeField] private int SkillPurchasedSfx;
        [KTTag("AudioTag")] [SerializeField] private int NotAffordableSfx;
        
        private static readonly int Purchased = Animator.StringToHash("Purchased");

        private static readonly int Selected = Animator.StringToHash("Selected");
        
        //Runtime 
        [NonSerialized] public TraitUpgradesMenu ParentMenu;
        
        public override void Initialize()
        {
            base.Initialize();
            if (UpgradeDataAsset == null) return;
            if (Icon != null) Icon.sprite = UpgradeDataAsset.GetIcon();
            if(Name != null) Name.text = UpgradeDataAsset.GetName();
            if (PriceTag != null)
            {
                BuyableInfo bi = UpgradeDataAsset.GetBuyableInfo();
                if (bi == null)
                {
                    if(PriceTag != null) PriceTag.gameObject.SetActive(false);
                }
                if (bi != null && PriceTag != null)
                {
                    PriceTag.gameObject.SetActive(true);
                    PriceTag.SetPriceInfo(bi, Rank,Rank-1);
                }
            }

            if (UpgradeButtonPanel != null)
            {
                UpgradeButtonPanel.Initialize(this);
                UpgradeButtonPanel.SetProgressable(UpgradeDataAsset, Rank);
            }
            UpdateVisualState();
        }
        
        private void OnEnable()
        {
            UpdateVisualState();
        }
    
        public void OnClick()
        {
            if (CurrentState == UnlockableStates.Locked) return;
            if(UpgradeDataAsset == null) return;

            if (UpgradeButtonPanel != null)
            {
                ParentMenu.SelectButton(this);
                return;
            }
            
            //No upgrade panel, just buy
            if (!ProgressionManager.BuyRank(UpgradeDataAsset, Rank))
            {
                AudioLibrary.PlaySoundByTag(NotAffordableSfx);
                return;
            }
            
            OnRankPurchased();
        }

        public void OnRankPurchased()
        {
            UpdateVisualState();
            AudioLibrary.PlaySoundByTag(SkillPurchasedSfx);
            if (Animator != null)
            {
                Animator.SetTrigger(Purchased);
            }
            ParentMenu.DeselectButton();
        }

        #region Visuals

        private void UpdateConnectorLine()
        {
            if (LineRenderer != null && DependsOn != null)
            {
                List<RectTransform> controlPoints = new List<RectTransform>();
                LineRenderer.gameObject.SetActive(true);
                LineRenderer.StartRect = DependsOn.GetComponent<RectTransform>();
                LineRenderer.EndRect = GetComponent<RectTransform>();
                if (!controlPoints.IsNullOrEmpty())
                {
                    foreach (var controlPoint in ConnectorControlPoints)
                    {
                        if (controlPoint != null)
                        {
                            controlPoints.Add(controlPoint);
                        }
                    }    
                }
                controlPoints.Add(GetComponent<RectTransform>());
                LineRenderer.ControlPoints = controlPoints;

            }else if (LineRenderer != null && DependsOn == null)
            {
                LineRenderer.gameObject.SetActive(false);
            }

        }
        /// <summary>
        /// Updates the visual state
        /// </summary>
        public void UpdateVisualState()
        {
            UpdateConnectorLine();
            bool rankPurchased = ProgressionManager.IsRankUnlocked(UpgradeDataAsset, Rank);
            bool conditionsMet = ProgressionManager.IsRankConditionSatisfied(UpgradeDataAsset, Rank);
            
            CurrentState = rankPurchased ? UnlockableStates.Unlocked : (conditionsMet ? UnlockableStates.Purchasable : UnlockableStates.Locked);
       
            if(LockedState != null) LockedState.SetActive(!rankPurchased && !conditionsMet);
            if(UnlockedState != null) UnlockedState.SetActive(rankPurchased);
            if(PurchasableState != null) PurchasableState.SetActive(!rankPurchased && conditionsMet);

            if (UpgradeButtonPanel != null)
            {
                if(rankPurchased) UpgradeButtonPanel.SetPurchasedState();
                else UpgradeButtonPanel.SetPurchasableState();
            }
            
            UpdateLineVisuals();
        }

        private void UpdateLineVisuals()
        {
            if(LockedLine != null) LockedLine.SetActive(false);
            if(PurchasableLine != null) PurchasableLine.SetActive(false);
            if(UnlockedLine != null) UnlockedLine.SetActive(false);

            switch (CurrentState)
            {
                case UnlockableStates.Unlocked:
                    if (UnlockedLine != null) UnlockedLine.SetActive(true);
                    break;
                case UnlockableStates.Purchasable:
                    if(PurchasableLine != null) PurchasableLine.SetActive(true);
                    break;
                case UnlockableStates.Locked:
                    if (LockedLine != null) LockedLine.SetActive(true);
                    break;
            }
        }
        #endregion

        #region Connections
        /// <summary>
        /// Connects to another button
        /// </summary>
        /// <param name="button"></param>
        public void ConnectToButton(UpgradeTreeButton button)
        {
            if (button == null) return;
            if (DependsOn != null) return; //Already connected
            DependsOn = button;
            UpdateConnectorLine();
        }
        #endregion

        #region Events

        public void OnSelected()
        {
            if (UpgradeButtonPanel != null)
            {
                UpgradeButtonPanel.Open();
            }

            if (Animator != null)
            {
                Animator.SetTrigger(Selected);
            }
        }

        public void OnDeselected()
        {
            if (UpgradeButtonPanel != null)
            {
                UpgradeButtonPanel.Close();
            }
        }

        #endregion
    }
}