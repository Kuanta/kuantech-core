using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.UI
{
    public class TabGroup : MonoBehaviour {
        
        [SerializeField] private List<TabGroupButton> TabGroupButtons;
        [SerializeField] private List<TabGroupTab> Tabs;

        private int _currentTabIndex = 0;

        private void Start()
        {
            for(int i=0;i<TabGroupButtons.Count;++i)
            {
                TabGroupButtons[i].ButtonIndex = i;
                TabGroupButtons[i].OnButtonPressed += OnTabButtonClicked;
            }

            ToggleTab(0);
        }

        public int GetCurrentTabIndex()
        {
            return _currentTabIndex;
        }

        private void OnTabButtonClicked(object sender, int index)
        {
            if(index == _currentTabIndex) return;
           ToggleTab(index);
        }

        private void ToggleTab(int index)
        {
            _currentTabIndex = index;
            for (int i = 0; i < TabGroupButtons.Count; ++i)
            {
                TabGroupButtons[i].Toggle(i == index);
                if(i == index)
                {
                    Tabs[i].Show();
                }else{
                    Tabs[i].Close();
                }
            }
        }
    }
}