using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.UI
{
    public class MenuOpenButton : MonoBehaviour, IUIButtonAction
    {
        [SerializeField] private MenuGroup MenuGroup;
        [SerializeField] private UIMenu MenuToOpen;
        [SerializeField] private string MenuIDToOpen;

        [Header("Menu States")] [SerializeField]
        private GameObject OpenedStateVisual;
        [SerializeField] private GameObject ClosedStateVisual;
        private void Start()
        {
            if (MenuGroup != null)
            {
                MenuGroup.OnMenuOpened += OnMenuOpened;
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
            if(OpenedStateVisual != null) OpenedStateVisual.SetActive(isOpened);
            if(ClosedStateVisual != null) ClosedStateVisual.SetActive(!isOpened);
        }
    }
}