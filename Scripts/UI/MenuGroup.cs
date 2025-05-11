using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.UI
{
    public class MenuGroup : MonoBehaviour
    {
        [Header("Child Menus")]
        public List<UIMenu> Menus;

        public UnityAction<UIMenu> OnMenuOpened;
        
        /// <summary>
        /// Opens a menu
        /// </summary>
        public void OpenMenu(UIMenu menuToOpen)
        {
            foreach (var menu in Menus)
            {
                if(menu == menuToOpen) continue;
                menu.Close();
            }
            menuToOpen.Open();
            OnMenuOpened?.Invoke(menuToOpen);
        }

        public void OpenMenu(string menuId)
        {
            UIMenu menuToOpen = null;
            foreach (var menu in Menus)
            {
                if (menu.MenuId.Equals(menuId))
                {
                    menuToOpen = menu;
                    continue;
                }
            }

            if (menuToOpen == null) return;
            OpenMenu(menuToOpen);
        }
    }
}