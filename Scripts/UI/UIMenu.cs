using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class UIMenu : KtUIElement
    {
        [Header("UI Menu")]
        public string MenuId;
        public bool IsPopup = false;
        [Tooltip("Closes and pops all previous menu stacks")] public bool ClearStackOnOpen = true;
        [SerializeField] protected Button CloseButton;
        
        //Animations
        private bool _initialized = false;

        protected virtual void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(() =>
                {
                    UIManager.GetContext<UIManager>().PopFromStack(this);
                });
            }
        }
        
        protected virtual void Start()
        {
           Initialize();
        }
        
        public override void Open()
        {
            Initialize();
            base.Open();
            UIManager.GetContext<UIManager>().PushToStack(this, false); //Don't call open again
        }

        public override void Close()
        {
            if (UIManager.GetTopMenu() != this)
            {
                Debug.LogWarning($"Menu {MenuId} tried to close itself while not being on top of stack.");
                return;
            }
            base.Close();
            UIManager.GetContext<UIManager>().PopFromStack(this, false); //Don't call close again
        }
    }
}