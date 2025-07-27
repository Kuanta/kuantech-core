using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.UI
{
    public class MenuOpenButton : MonoBehaviour, KtButton.IUIButtonAction
    {
        [SerializeField] private MenuGroup MenuGroup;
        [SerializeField] private UIMenu MenuToOpen;
        [SerializeField] private string MenuIDToOpen;
        [Header("Menu States")] 
        [SerializeField] private GameObject OpenedStateVisual;
        [SerializeField] private GameObject ClosedStateVisual;
        [SerializeField] private Animator Animator;
        private static readonly int Opened = Animator.StringToHash("Opened");

        private void Start()
        {
            if (MenuGroup != null)
            {
                MenuGroup.OnMenuOpened += OnMenuOpened;
            }

            UIMenu menu = MenuToOpen;
            if (menu == null && MenuIDToOpen != null)
            {
                menu = UIManager.GetMenuById(MenuIDToOpen);
            }

            if (menu != null)
            {
                menu.OnMenuOpened += SetOpenedVisual;
                menu.OnMenuClosed += SetClosedVisual;
            }

            if (menu.IsVisible())
            {
                SetOpenedVisual();
            }
            else
            {
                SetClosedVisual();
            }
        }
        
        public void OnClick()
        {
            if (MenuToOpen != null)
            {
                if (MenuGroup != null)
                {
                    MenuGroup.OpenMenu(MenuToOpen);
                }
                else
                {
                    UIManager.OpenMenu(MenuToOpen);
                }
            }else if (!MenuIDToOpen.IsNullOrEmpty())
            {
                if (MenuGroup != null)
                {
                    MenuGroup.OpenMenu(MenuIDToOpen);
                }
                else
                {
                    UIManager.OpenMenu(MenuIDToOpen);
                }
            }
            else
            {
                Debug.LogWarning("Menu to open is not set in MenuOpenButton. Please set it in the inspector or use MenuIDToOpen.");
            }
        }

        private void OnMenuOpened(UIMenu menu)
        {
            bool isOpened = MenuToOpen == menu || menu.MenuId.Equals(MenuIDToOpen);
            if (isOpened)
            {
                SetOpenedVisual();
            }
            else
            {
                SetClosedVisual();
            }
        }

        private void SetOpenedVisual()
        {
            if(OpenedStateVisual != null) OpenedStateVisual.SetActive(true);
            if(ClosedStateVisual != null) ClosedStateVisual.SetActive(false);
            if(Animator != null) Animator.SetBool(Opened, true);
        }

        private void SetClosedVisual()
        {
            if(OpenedStateVisual != null) OpenedStateVisual.SetActive(false);
            if(ClosedStateVisual != null) ClosedStateVisual.SetActive(true);
            if(Animator != null) Animator.SetBool(Opened, false);
        }
    }
}