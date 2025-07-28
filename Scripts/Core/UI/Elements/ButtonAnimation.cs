using UnityEngine;

namespace Kuantech.Core.UI
{
    public class ButtonAnimation :  MonoBehaviour, KtButton.IUIButtonAction
    {
        [SerializeField] private Animator ButtonAnimator;
        private static readonly int Clicked = Animator.StringToHash("Clicked");

        public void OnClick()
        {
            if (ButtonAnimator == null) return;
            ButtonAnimator.SetTrigger(Clicked);
        }
    }
}