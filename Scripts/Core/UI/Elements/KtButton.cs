using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class KtButton : Button
    {
        public UnityAction<KtButton> OnPreButtonClicked; //Before calling actions
        public UnityAction<KtButton> OnPostButtonClicked; //After calling actions
        
        public interface IUIButtonAction
        {
            void OnClick();
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
    }
}