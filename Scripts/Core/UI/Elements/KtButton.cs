using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public interface IUIButtonAction
    {
        void OnClick();
    }
    
    [RequireComponent(typeof(Button))]
    public class KtButton : MonoBehaviour
    {
        private Button _button;
        private IUIButtonAction[] _actions;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _actions = GetComponents<IUIButtonAction>();
            _button.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            foreach (var action in _actions)
            {
                action.OnClick();
            }
        }
    }
}