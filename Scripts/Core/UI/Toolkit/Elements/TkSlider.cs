using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// A horizontal slider from 0 to 1 that is dragged with the pointer (mouse or finger). It only does the behaviour
    /// and the layout; how it looks is up to the theme USS, through these classes:
    ///   kt-slider, kt-slider__track, kt-slider__fill, kt-slider__handle
    ///
    /// In UXML: <c>&lt;Kuantech.Core.UI.TkSlider value="0.5" /&gt;</c>
    /// </summary>
    [UxmlElement]
    public partial class TkSlider : VisualElement
    {
        /// <summary>Raised when the player changes the value. Setting <see cref="Value"/> from code does not raise it.</summary>
        public event Action<float> ValueChanged;

        private readonly VisualElement _track;
        private readonly VisualElement _fill;
        private readonly VisualElement _handle;
        private float _value;

        /// <summary>0 to 1. Shown straight away; does not raise <see cref="ValueChanged"/>.</summary>
        [UxmlAttribute]
        public float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp01(value);
                Refresh();
            }
        }

        public TkSlider()
        {
            AddToClassList("kt-slider");
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            // The track is the part the pointer maps onto. The fill and the handle are inside it, so percentages
            // of the track are percentages of the slider.
            _track = new VisualElement { pickingMode = PickingMode.Ignore };
            _track.AddToClassList("kt-slider__track");
            _track.style.flexGrow = 1;

            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            _fill.AddToClassList("kt-slider__fill");
            _fill.style.height = Length.Percent(100);

            _handle = new VisualElement { pickingMode = PickingMode.Ignore };
            _handle.AddToClassList("kt-slider__handle");
            _handle.style.position = Position.Absolute;
            _handle.style.top = Length.Percent(50);
            _handle.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));

            _track.Add(_fill);
            _track.Add(_handle);
            Add(_track);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            Refresh();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            this.CapturePointer(evt.pointerId);
            SetFromPointer(evt.position.x);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId)) return;
            SetFromPointer(evt.position.x);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (this.HasPointerCapture(evt.pointerId)) this.ReleasePointer(evt.pointerId);
        }

        private void SetFromPointer(float panelX)
        {
            Rect bounds = _track.worldBound;
            if (bounds.width <= 0f) return;

            float value = Mathf.Clamp01((panelX - bounds.xMin) / bounds.width);
            if (Mathf.Approximately(value, _value)) return;

            _value = value;
            Refresh();
            ValueChanged?.Invoke(_value);
        }

        private void Refresh()
        {
            _fill.style.width = Length.Percent(_value * 100f);
            _handle.style.left = Length.Percent(_value * 100f);
        }
    }
}
