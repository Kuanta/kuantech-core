using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class UIMenu : UIElement
    {
        [Header("UI Menu")]
        public string MenuId;
        public bool IsPopup = false;
        [Tooltip("Closes and pops all previous menu stacks")] public bool ClearStackOnOpen = true;
        [SerializeField] protected Button CloseButton;
        
        //Animations
        protected bool Initialized = false;

        public virtual void Initialize()
        {
            if (Initialized) return;
            Initialized = true;
            
            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(() =>
                {
                    UIManager.GetContext<UIManager>().PopFromStack(this);
                });
            }
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