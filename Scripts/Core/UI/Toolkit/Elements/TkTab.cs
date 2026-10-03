using UnityEngine.UIElements;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// One tab of a <see cref="TkTabBar"/>. What it shows is put inside it in UXML (usually an icon element); the label
    /// and the notification dot belong to the tab itself. The look is the theme's: classes
    ///   kt-tab, kt-tab--selected, kt-tab__label, kt-tab__badge
    ///
    /// In UXML: <c>&lt;Kuantech.Core.UI.TkTab label="Deck"&gt;&lt;ui:VisualElement class="my-icon" /&gt;&lt;/Kuantech.Core.UI.TkTab&gt;</c>
    /// </summary>
    [UxmlElement]
    public partial class TkTab : VisualElement
    {
        public const string SelectedClass = "kt-tab--selected";

        private readonly Label _label;
        private readonly VisualElement _badge;
        private bool _hasBadge;

        /// <summary>The text under the icon.</summary>
        [UxmlAttribute]
        public string Label
        {
            get => _label.text;
            set => _label.text = value;
        }

        /// <summary>Shows the red notification dot on the tab.</summary>
        [UxmlAttribute]
        public bool HasBadge
        {
            get => _hasBadge;
            set
            {
                _hasBadge = value;
                _badge.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
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

            // The children from the UXML are added after the constructor. Moving the label to the end once the tab
            // is in a panel puts it under them (the icon above, the text below).
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                _label.BringToFront();
                _badge.BringToFront();
            });
        }

        internal void SetSelected(bool selected) => EnableInClassList(SelectedClass, selected);
    }
}
