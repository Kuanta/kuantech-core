using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class KtButton : Button
    {
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
            foreach (var action in _actions)
            {
                action.OnClick();
            }
        }
    }
}