using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class KtButton : Button
    {
        public UnityAction<KtButton> OnPreButtonClicked; //Before calling actions
        public UnityAction<KtButton> OnPostButtonClicked; //After calling actions

        public UnityAction<KtButton> OnPositiveEffect;
        public UnityAction<KtButton> OnNegativeEffect;

        public interface IUIButtonAction
        {
            public void OnClick();

            public void PositiveEffect()
            {
            }

            public void NegativeEffect()
            {
            }
        }
    
        private IUIButtonAction[] _actions;

        protected override void Awake()
        {
            base.Awake();
            _actions = GetComponents<IUIButtonAction>();
            onClick.AddListener(NotifyActions);
        }

        private void NotifyActions()
        {
            OnPreButtonClicked?.Invoke(this);
            foreach (var action in _actions)
            {
                action.OnClick();
            }
            OnPostButtonClicked?.Invoke(this);
        }

        public void TriggerPositiveEffect()
        {
            
        }
    }
}