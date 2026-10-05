using UnityEngine;
using UnityEngine.UIElements;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// A bar that shows how full something is, from 0 to 1, with an optional badge at its left end (a level number). It only shows: it takes no
    /// input. It only does the behaviour and the layout; how it looks is up to the theme USS, through these classes:
    ///   kt-progress, kt-progress__track, kt-progress__fill-area, kt-progress__fill, kt-progress__badge, kt-progress__badge-text
    ///
    /// In UXML: <c>&lt;Kuantech.Core.UI.TkProgressBar value="0.5" badge-text="3" /&gt;</c>
    /// </summary>
    [UxmlElement]
    public partial class TkProgressBar : VisualElement
    {
        private readonly VisualElement _fill;
        private readonly VisualElement _badge;
        private readonly Label _badgeText;
        private float _value;
        private string _badgeString = string.Empty;

        /// <summary>0 to 1 (it is clamped). Shown straight away. An empty bar (0) shows no fill at all.</summary>
        [UxmlAttribute]
        public float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp01(value);
                RefreshFill();
            }
        }

        /// <summary>The text in the badge at the left end (empty: no badge).</summary>
        [UxmlAttribute]
        public string BadgeText
        {
            get => _badgeString;
            set
            {
                _badgeString = value ?? string.Empty;
                _badgeText.text = _badgeString;
                _badge.style.display = _badgeString.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public TkProgressBar()
        {
            AddToClassList("kt-progress");
            pickingMode = PickingMode.Ignore;

            // The track is the frame; the fill area is the room inside it that the fill grows in, so a percentage of it is the value.
            VisualElement track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.AddToClassList("kt-progress__track");

            VisualElement fillArea = new VisualElement { pickingMode = PickingMode.Ignore };
            fillArea.AddToClassList("kt-progress__fill-area");

            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            _fill.AddToClassList("kt-progress__fill");
            _fill.style.height = Length.Percent(100);

            fillArea.Add(_fill);
            track.Add(fillArea);
            Add(track);

            // The badge is added after the track, so it is drawn over the end of it.
            _badge = new VisualElement { pickingMode = PickingMode.Ignore };
            _badge.AddToClassList("kt-progress__badge");
            _badgeText = new Label { pickingMode = PickingMode.Ignore };
            _badgeText.AddToClassList("kt-progress__badge-text");
            _badge.Add(_badgeText);
            Add(_badge);

            BadgeText = string.Empty;
            RefreshFill();
        }

        private void RefreshFill()
        {
            _fill.style.display = _value <= 0f ? DisplayStyle.None : DisplayStyle.Flex;
            _fill.style.width = Length.Percent(_value * 100f);
        }
    }
}
