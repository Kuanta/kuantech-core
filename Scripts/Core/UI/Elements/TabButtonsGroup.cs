using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.UI
{
    public class TabButtonsGroup : MonoBehaviour
    {
        [SerializeField] private List<TabButton> ToggleButtons;
        [SerializeField] private TabButton DefaultButton;
        
        private void Awake()
        {
            if (ToggleButtons.IsNullOrEmpty()) return;
            foreach (var button in ToggleButtons)
            {
                button.Initialize(this);
            }

            if (DefaultButton != null)
            {
                OnChildButtonClicked(DefaultButton);
            }
            else
            {
                OnChildButtonClicked(ToggleButtons[0]);
            }
        }

        public void OnChildButtonClicked(TabButton childButton)
        {
            foreach (var button in ToggleButtons)
            {
                if(childButton == button)
                {
                    button.OpenTab();
                }
                else
                {
                    button.CloseTab();
                }
            }
        }
    }
}