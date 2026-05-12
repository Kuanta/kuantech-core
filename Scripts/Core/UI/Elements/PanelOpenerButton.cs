using UnityEngine;
using static Kuantech.Core.UI.KtButton;

namespace Kuantech.Core.UI
{
    public class PanelOpenerButton : MonoBehaviour, IUIButtonAction
    {
        public string PanelId;
        public void OnClick()
        {
            UIManager.OpenPanel(PanelId);
        }
    }
}