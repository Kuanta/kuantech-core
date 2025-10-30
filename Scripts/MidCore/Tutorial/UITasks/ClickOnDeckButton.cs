using Kuantech.Core.UI;
using Kuantech.Midcore.UI;

namespace Kuantech.Midcore.Tutorial
{
    public class ClickOnDeckButton : ClickOnButtonTask
    {
        public DeckSelectionMenu DeckSelectionMenu;
        
        public override void StartTask()
        {
            KtButton buttonToClick = null;
            ButtonFocusToggler focusToggler = null;
            foreach (var deckBucollectibleCard in DeckSelectionMenu.DeckCards)
            {
                if (deckBucollectibleCard.CollectibleDataAsset == null)
                {
                    buttonToClick = deckBucollectibleCard.GetComponentInChildren<KtButton>();
                    focusToggler = deckBucollectibleCard.GetComponentInChildren<ButtonFocusToggler>();
                    break;
                }
            }

            if (buttonToClick == null)
            {
                var deckBucollectibleCard = DeckSelectionMenu.DeckCards[1]; 
                buttonToClick = deckBucollectibleCard.GetComponentInChildren<KtButton>();
                focusToggler = deckBucollectibleCard.GetComponentInChildren<ButtonFocusToggler>();
            }

            ButtonToClick = buttonToClick;
            FocusToggler = focusToggler;
            
            base.StartTask();
        }
        
    }
}