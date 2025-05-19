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
    public class UpgradeTreeButton : KtUIElement, IUIButtonAction
    {
        public enum ButtonState
        {
            Locked, //Can't be bought
            Unlocked, //Bought
            Purchasable, //Can be bought
        }

        [NonSerialized] public ButtonState CurrentState;
        
        [Header("Progression Asset")] 
        public UpgradeDataAsset UpgradeDataAsset;
        public int Rank;

        [Header("Common Visuals")] 
        [SerializeField] private Image Icon;
        [SerializeField] private TMP_Text Name;
        [SerializeField] private PricePanel PriceTag;
        
        [Header("Connection")] 
        [SerializeField] private ConnectorUILine LineRenderer;
        [SerializeField] private UpgradeTreeButton DependsOn;
        [SerializeField] private List<RectTransform> ConnectorControlPoints;
        
        [Header("Visual States")]
        [SerializeField] private GameObject LockedState;
        [SerializeField] private GameObject PurchasableState;
        [SerializeField] private GameObject UnlockedState;

        [Header("Effects & Anims")] [SerializeField]
        private Animator Animator;
        [KTTag("AudioTag")] [SerializeField] private int SkillPurchasedSfx;
        [KTTag("AudioTag")] [SerializeField] private int NotAffordableSfx;
        private static readonly int Purchased = Animator.StringToHash("Purchased");

        public void Initialize()
        {
            if (UpgradeDataAsset == null) return;
            if (Icon != null) Icon.sprite = UpgradeDataAsset.Icon;
            if(Name != null) Name.text = UpgradeDataAsset.Name;
            if (PriceTag != null)
            {
                BuyableInfo bi = StoreManager.GetContext<StoreManager>()?.GetBuyableInfo(UpgradeDataAsset.StoreEntryId);
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
        }
        private void OnEnable()
        {
            UpdateVisualState();
        }
    
        public void OnClick()
        {
            if (CurrentState != ButtonState.Purchasable) return;
            if(UpgradeDataAsset == null) return;
            
            //Try to buy the progression
            if (!ProgressionManager.BuyRank(UpgradeDataAsset, Rank))
            {
                AudioLibrary.PlaySoundByTag(NotAffordableSfx);
                return;
            }
            
            OnRankPurchased();
        }

        private void OnRankPurchased()
        {
            UpdateVisualState();
            AudioLibrary.PlaySoundByTag(SkillPurchasedSfx);
            if (Animator != null)
            {
                Animator.SetTrigger(Purchased);
            }
        }

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
            bool conditionsMet = ProgressionManager.IsRankUpgradeConditionsMet(UpgradeDataAsset, Rank);
            
            CurrentState = rankPurchased ? ButtonState.Unlocked : (conditionsMet ? ButtonState.Purchasable : ButtonState.Locked);
       
            if(LockedState != null) LockedState.SetActive(!rankPurchased && !conditionsMet);
            if(UnlockedState != null) UnlockedState.SetActive(rankPurchased);
            if(PurchasableState != null) PurchasableState.SetActive(!rankPurchased && conditionsMet);
        }
        
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
    }
}