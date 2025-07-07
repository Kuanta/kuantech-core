using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class UIMenu : UIElement
    {
        [Header("UI Menu")]
        public string MenuId;
        public bool IsPopup = false;
        [Tooltip("Closes and pops all previous menu stacks")] 
        public bool ClearStackOnOpen = true;
        [SerializeField] protected Button CloseButton;
        
        //Animations

        public override void Initialize()
        {
            if (Initialized) return;

            base.Initialize();
            
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
            if(!Initialized) Initialize();
            base.Open();
            UIManager.GetContext<UIManager>().PushToStack(this, false); //Don't call open again
        }

        public override void Close()
        {
            base.Close();
            UIManager.GetContext<UIManager>().PopFromStack(this, false); //Don't call close again
        }
    }
}