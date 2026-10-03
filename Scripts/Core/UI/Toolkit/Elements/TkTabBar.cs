using System;
using UnityEngine.UIElements;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// A row of <see cref="TkTab"/>s of which one is selected. The bar only reports the press; whoever owns it decides
    /// whether the tab can be selected and calls <see cref="Select"/>. A tab that is not allowed (a closed shop) simply
    /// never gets selected, and the owner can play an error feedback for it.
    ///
    /// In UXML: <c>&lt;Kuantech.Core.UI.TkTabBar&gt; (TkTab children) &lt;/Kuantech.Core.UI.TkTabBar&gt;</c>
    /// </summary>
    [UxmlElement]
    public partial class TkTabBar : VisualElement
    {
        /// <summary>A tab was pressed (not necessarily selected). Argument: its index among the tabs.</summary>
        public event Action<int> TabPressed;

        public int SelectedIndex { get; private set; } = -1;

        public TkTabBar()
        {
            AddToClassList("kt-tab-bar");
            style.flexDirection = FlexDirection.Row;
            RegisterCallback<ClickEvent>(OnClick);
        }

        /// <summary>The tab at this index, or null.</summary>
        public TkTab GetTab(int index)
        {
            int i = 0;
            foreach (VisualElement child in Children())
            {
                if (child is not TkTab tab) continue;
                if (i == index) return tab;
                i++;
            }
            return null;
        }

        public int IndexOf(TkTab target)
        {
            int i = 0;
            foreach (VisualElement child in Children())
            {
                if (child is not TkTab tab) continue;
                if (tab == target) return i;
                i++;
            }
            return -1;
        }

        /// <summary>Selects a tab. Does not raise <see cref="TabPressed"/>.</summary>
        public void Select(int index)
        {
            int i = 0;
            foreach (VisualElement child in Children())
            {
                if (child is not TkTab tab) continue;
                tab.SetSelected(i == index);
                i++;
            }
            SelectedIndex = index;
        }

        private void OnClick(ClickEvent evt)
        {
            // The press lands on whatever is under the finger (the icon, say); the tab is its ancestor.
            VisualElement element = evt.target as VisualElement;
            while (element != null && element is not TkTab) element = element.parent;
            if (element is not TkTab tab) return;

            int index = IndexOf(tab);
            if (index >= 0) TabPressed?.Invoke(index);
        }
    }
}
