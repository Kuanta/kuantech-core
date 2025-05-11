using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.UI
{
    public class MenuOpenButton : MonoBehaviour, IUIButtonAction
    {
        [SerializeField] private UIMenu MenuToOpen;
        [SerializeField] private string MenuIDToOpen;
        public void OnClick()
        {
            if (MenuToOpen != null)
            {
                UIManager.OpenMenu(MenuToOpen);
            }else if (!MenuIDToOpen.IsNullOrEmpty())
            {
                UIManager.OpenMenu(MenuIDToOpen);
            }
        }
    }
}