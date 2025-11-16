using Kuantech.Core;
using Kuantech.Core.UI;
using Kuantech.Midcore.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore.Tutorial
{
    public class ClickOnCollectibleCardTask : ClickOnButtonTask
    {
        public enum ButtonToClickType
        {
            CollectibleCard,
            InfoButton,
            EquipButton,
        }
        
        [Header("Click On Collectible")]
        public MetadataAsset MetadataAsset;
        public Transform CardsParent;
        public ButtonToClickType ButtonType;
        
        public override void StartTask()
        {
            //Find the button
            CollectiblePreviewCard[] collectibleCards = CardsParent.GetComponentsInChildren<CollectiblePreviewCard>();
            if (collectibleCards.IsNullOrEmpty())
            {
                CompleteTask();
                return;
            }

            foreach (var card in collectibleCards)
            {
                if (card.CollectibleDataAsset == MetadataAsset)
                {
                    SetButtonToClick(card);
                    base.StartTask();
                    return;
                }
            }
            
            //Couldn't find the card
            CompleteTask();
        }

        private void SetButtonToClick(CollectiblePreviewCard card)
        {
            KtButton cardBtn = null;
            
            switch (ButtonType)
            {
                
                case ButtonToClickType.EquipButton:
                    cardBtn = card.GetEquipButton();
                    break;
                case ButtonToClickType.InfoButton:
                    cardBtn = card.GetInfoButton();
                    break;
                case ButtonToClickType.CollectibleCard:
                default:
                    cardBtn = card.GetCardButton();
                    break;
            }
            
            ButtonToClick = cardBtn;
            FocusToggler = cardBtn.GetComponent<ButtonFocusToggler>();
        }
    }
}