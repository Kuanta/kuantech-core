
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// A panel is a UI element that is outside the navigation system. Like UI inventory, 
    /// stats menu etc.
    /// </summary>
    public class KtUIPanel : UIElement
    {
        [Header("Panel")]
        public string PanelId;
        [SerializeField] private Button CloseButton;

        public override void Initialize()
        {
            if(Initialized) return;
        
            base.Initialize();

            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(Close);
            }

        }
    }
}