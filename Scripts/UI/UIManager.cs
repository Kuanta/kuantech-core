using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.UI
{
    public class UIManager : SubManager
    {
        private Stack<UIMenu> _menuStack = new Stack<UIMenu>();
        
        [Header("Default Menu")]
        [SerializeField] private UIMenu _defaultMenu;

        private HashSet<UIMenu> _staticMenus = new HashSet<UIMenu>();
        private Dictionary<string, UIMenu> _menusById = new Dictionary<string, UIMenu>();

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            _staticMenus = GetComponentsInChildren<UIMenu>().ToList().ToHashSet();
            foreach (var menu in _staticMenus)
            {
                if(menu.MenuId.IsNullOrEmpty()) continue;
                _menusById[menu.MenuId] = menu;
            }
            _menuStack = new Stack<UIMenu>();
        }
        
        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            foreach(var menu in _staticMenus)
            {
                menu.gameObject.SetActive(false);
            }
            
            //Open default menu
            if (_defaultMenu != null)
            {
                PushToStack(_defaultMenu);
            }
        }

        #region Menu Manupilation

        public static void OpenMenu(string menuId)
        {
            var context = GetContext<UIManager>();
            OpenMenu(context.GetMenuById(menuId));
        }
        public static void OpenMenu(UIMenu menu)
        {
            if (menu == null) return;
            var ctx = GetContext<UIManager>();
            ctx.PushToStack(menu);
        }
        public static void CloseMenu(string menuId)
        {
            var context = GetContext<UIManager>();
            CloseMenu(context.GetMenuById(menuId));
        }
        public static void CloseMenu(UIMenu menu)
        {
            if (menu == null) return;
            var ctx = GetContext<UIManager>();
            ctx.PopFromStack(menu);
        }

        public UIMenu GetMenuById(string menuId)
        {
            if (_menusById.IsNullOrEmpty() || !_menusById.ContainsKey(menuId)) return null;
            return _menusById[menuId];
        }
        #endregion
        
        #region Navigation

        public static UIMenu GetTopMenu()
        {
            var context = GetContext<UIManager>();
            if (context == null) return null;
            return context._menuStack.Peek();
        }
        
        public void PushToStack(UIMenu menu, bool callOpen = true)
        {
            if (_menuStack.Count > 0 && _menuStack.Peek() == menu)
                return;

            if (!_staticMenus.Contains(menu))
            {
                Debug.LogWarning("Trying to push a menu that's not in static set.");
            }

            if (menu.ClearStackOnOpen)
            {
                ClearStack();
            }
            
            if (_menuStack.Count > 0)
            {
                var current = _menuStack.Peek();

                if (!menu.IsPopup)
                {
                    current.Hide(); // Öncekini sakla
                }
            }

            _menuStack.Push(menu);
            if(callOpen) menu.Open();
        }
 
        public void PopFromStack(UIMenu menu, bool callClose = true)
        {
            if (_menuStack.Peek() != menu) return;
            UIMenu menuToClose = _menuStack.Pop();
            
            if(callClose) menuToClose.Close();
        }

        public void ClearStack()
        {
            while (_menuStack.Count > 1)
            {
                var menu = _menuStack.Pop();
                menu.Hide();
            }
        }
        #endregion
    }
}