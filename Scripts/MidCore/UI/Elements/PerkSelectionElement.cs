using System;
using Kuantech.Core.UI;
using Kuantech.Rpg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    public class PerkSelectionElement : UIElement
    {
        [Header("Buttons")]
        [SerializeField] private KtButton SelectPerkButton;
        
        [Header("Visual Fields")]
        [SerializeField] private TMP_Text PerkNameText;
        [SerializeField] private TMP_Text PerkRankText;
        [SerializeField] private Image Icon;
        [SerializeField] private IconRankIndicator IconRankIndicator;
        [SerializeField] private TMP_Text PerkDescriptionText;

        [NonSerialized] public PerkSelectionPanel ParentPanel;
        [NonSerialized] public Perk CurrentPerk;
        private static readonly int Selected = Animator.StringToHash("Selected");
        private static readonly int Unselected = Animator.StringToHash("Unselected");

        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            
            SelectPerkButton.onClick.AddListener(SelectPerk);    
        }
        
        public void SetPerk(Perk perk)
        {
            if(PerkNameText != null) PerkNameText.text = perk.PerkAsset.GetName();
            if (Icon != null) Icon.sprite = perk.PerkAsset.GetIcon();
            if(PerkRankText != null) PerkRankText.text = $"{perk.CurrentRank + 1}";
            if(IconRankIndicator != null) IconRankIndicator.SetRank(perk.CurrentRank);
            if(PerkDescriptionText != null) PerkDescriptionText.text = perk.PerkAsset.BuildDescription(perk.CurrentRank);
            CurrentPerk = perk;
        }

        private void SelectPerk()
        {
            if (ParentPanel == null)
            {
                Debug.LogError("Parent perk selection panel is null");
                return;
            }

            ParentPanel.OnPerkSelected(this);
            
        }
        
        public void PlaySelectedAnimation()
        {
            if (ElementAnimator == null) return;
            ElementAnimator.SetTrigger(Selected);

        }
        
        public void PlayNotSelectedAnimation()
        {
            if (ElementAnimator == null) return;
            ElementAnimator.SetTrigger(Unselected);

        }
    }
}