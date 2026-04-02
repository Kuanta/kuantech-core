using UnityEngine;

namespace Kuantech.Core.UI
{
    public class ButtonAnimation :  MonoBehaviour, KtButton.IUIButtonAction
    {
        [SerializeField] private Animator ButtonAnimator;
        private static readonly int Clicked = Animator.StringToHash("Clicked");
        private static readonly int PositiveClicked = Animator.StringToHash("Positive");
        private static readonly int NegativeClicked = Animator.StringToHash("Negative");

        public void OnClick()
        {
            if (ButtonAnimator == null) return;
            ButtonAnimator.SetTrigger(Clicked);
        }
        
        public void PositiveEffect()
        {
            if (ButtonAnimator == null) return;
            ButtonAnimator.SetTrigger(PositiveClicked);
        }

        public void NegativeEffect()
        {
            if (ButtonAnimator == null) return;
            ButtonAnimator.SetTrigger(NegativeClicked);
        }
    }
}