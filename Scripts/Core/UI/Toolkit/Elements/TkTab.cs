using System;
using UnityEngine.UIElements;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// One tab of a <see cref="TkTabBar"/>. What it shows is put inside it in UXML (usually an icon element); the label
    /// and the alert belong to the tab itself. The alert is a red badge: a plain dot (<see cref="HasBadge"/>), or a dot with a
    /// number in it (<see cref="AlertCount"/>: "3 new things here"). The look is the theme's: classes
    ///   kt-tab, kt-tab--selected, kt-tab__label, kt-tab__badge, kt-tab__badge-count
    ///
    /// In UXML: <c>&lt;Kuantech.Core.UI.TkTab label="Deck"&gt;&lt;ui:VisualElement class="my-icon" /&gt;&lt;/Kuantech.Core.UI.TkTab&gt;</c>
    /// </summary>
    [UxmlElement]
    public partial class TkTab : VisualElement
    {
        public const string SelectedClass = "kt-tab--selected";

        private readonly Label _label;
        private readonly VisualElement _badge;
        private readonly Label _badgeCount;
        private bool _hasBadge;
        private int _alertCount;

        /// <summary>The biggest number the badge writes; anything above it shows as "9+" so the text always fits the dot.</summary>
        public const int MaxShownCount = 9;

        /// <summary>The text under the icon.</summary>
        [UxmlAttribute]
        public string Label
        {
            get => _label.text;
            set => _label.text = value;
        }

        /// <summary>Shows the red notification dot on the tab (without a number).</summary>
        [UxmlAttribute]
        public bool HasBadge
        {
            get => _hasBadge;
            set
            {
                _hasBadge = value;
                UpdateBadge();
            }
        }

        /// <summary>
        /// The number in the red badge: how many things need the player here (0 = none). With a count of 1 or more the badge is shown with
        /// the number in it, whatever <see cref="HasBadge"/> says; at 0 the badge is the dot or nothing, as <see cref="HasBadge"/> says.
        /// </summary>
        [UxmlAttribute]
        public int AlertCount
        {
            get => _alertCount;
            set
            {
                _alertCount = Math.Max(0, value);
                UpdateBadge();
            }
        }

        public bool IsSelected => ClassListContains(SelectedClass);

        public TkTab()
        {
            AddToClassList("kt-tab");

            _label = new Label { pickingMode = PickingMode.Ignore };
            _label.AddToClassList("kt-tab__label");
            Add(_label);

            _badge = new VisualElement { pickingMode = PickingMode.Ignore };
            _badge.AddToClassList("kt-tab__badge");
            _badge.style.position = Position.Absolute;
            _badge.style.display = DisplayStyle.None;
            Add(_badge);

            _badgeCount = new Label { pickingMode = PickingMode.Ignore };
            _badgeCount.AddToClassList("kt-tab__badge-count");
            _badge.Add(_badgeCount);

            // The children from the UXML are added after the constructor. Moving the label to the end once the tab
            // is in a panel puts it under them (the icon above, the text below).
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                _label.BringToFront();
                _badge.BringToFront();
            });
        }

        private void UpdateBadge()
        {
            bool counted = _alertCount > 0;
            _badge.style.display = counted || _hasBadge ? DisplayStyle.Flex : DisplayStyle.None;
            _badgeCount.style.display = counted ? DisplayStyle.Flex : DisplayStyle.None;
            if (counted) _badgeCount.text = _alertCount > MaxShownCount ? MaxShownCount + "+" : _alertCount.ToString();
        }

        internal void SetSelected(bool selected) => EnableInClassList(SelectedClass, selected);
    }
}
