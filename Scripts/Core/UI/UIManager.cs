using System.Collections.Generic;
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
        [SerializeField] private List<UIMenu> StaticMenusList;

        [SerializeField] private List<UIMenu> MenusToInitializeOnStart;
        private Dictionary<string, UIMenu> _menusById = new Dictionary<string, UIMenu>();

        [Header("Game UI")] 
        [SerializeField] private LevelUI LevelUI;
        
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            foreach (var menu in StaticMenusList)
            {
                if(menu.MenuId.IsNullOrEmpty()) continue;
                _menusById[menu.MenuId] = menu;
            }
            _menuStack = new Stack<UIMenu>();

            if (LevelUI != null)
            {
                LevelUI.Initialize();
            }
        }
        
        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            foreach(var menu in StaticMenusList)
            {
                menu.gameObject.SetActive(false);
            }
            
            //Open default menu
            if (_defaultMenu != null)
            {
                PushToStack(_defaultMenu);
            }
            
            foreach(var menu in MenusToInitializeOnStart)
            {
                if (menu == null) continue;
                menu.Initialize();
            }
        }

        public static UIMenu GetMenuById(string menuId)
        {
            var ctx = GetContext<UIManager>();
            return ctx._GetMenuById(menuId);
        }
        
        #region Menu Manupilation

        public static void OpenMenu(string menuId)
        {
            var context = GetContext<UIManager>();
            OpenMenu(context._GetMenuById(menuId));
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
            CloseMenu(context._GetMenuById(menuId));
        }
        public static void CloseMenu(UIMenu menu)
        {
            if (menu == null) return;
            var ctx = GetContext<UIManager>();
            ctx.PopFromStack(menu);
        }

        private UIMenu _GetMenuById(string menuId)
        {
            if (_menusById.IsNullOrEmpty() || !_menusById.ContainsKey(menuId)) return null;
            return _menusById[menuId];
        }
        #endregion
        
        #region Navigation

        public static UIMenu GetTopMenu()
        {
            var context = GetContext<UIManager>();
            if (context == null || context._menuStack.IsNullOrEmpty()) return null;
            return context._menuStack.Peek();
        }
        
        public void PushToStack(UIMenu menu, bool callOpen = true)
        {
            if (menu == null) return;
            if (_menuStack.Count > 0 && _menuStack.Peek() == menu)
                return;

            if (!StaticMenusList.Contains(menu))
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
            if (_menuStack.IsNullOrEmpty() || _menuStack.Peek() != menu) return;
            UIMenu menuToClose = _menuStack.Pop();
            if (_menuStack.Count == 1 && menu == _defaultMenu)
            {
                return;
            }
            if(callClose) menuToClose.Close();
            
            //If somehow stack is empty, push the default menu to stack
            if (_menuStack.IsNullOrEmpty())
            {
                PushToStack(_defaultMenu);
                return;
            }
            //Show the next on stack
            UIMenu menuOnTop = _menuStack.Peek();
            if (menuOnTop != null && !menuOnTop.IsVisible())
            {
                menuOnTop.Show();
            }
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

        #region Game UI
        /// <summary>
        /// Gets level UI
        /// </summary>
        /// <returns></returns>
        public static LevelUI GetLevelUI()
        {
            var ctx = LevelManager.GetContext<UIManager>();
            if (ctx == null) return null;
            return ctx.LevelUI;
        }

        #endregion
    }
}