using UnityEngine;
using UnityEngine.UIElements;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// Makes a ScrollView (or the one inside a ListView) scroll when it is dragged with a mouse or a pen, the way a finger scrolls
    /// it on a phone: the content follows the pointer, goes on for a while when let go, and gives like rubber at the ends (see
    /// <see cref="ScrollPhysics"/>). UI Toolkit scrolls by touch on its own but a mouse only has the wheel. A touch is left alone
    /// (the ScrollView handles it itself), so nothing scrolls twice.
    ///
    /// A press that moves less than <see cref="Threshold"/> is still a tap and reaches whatever is under it; once the drag starts
    /// the ScrollView takes the pointer, so the press does not turn into a tap on a card at the end.
    ///
    /// Use <see cref="TkScroll.Setup"/>, which also sets the ScrollView's own touch scrolling to the same feel.
    /// </summary>
    public class TkDragScroll : Manipulator
    {
        /// <summary>How far the pointer has to move (panel units) before the press counts as a drag.</summary>
        public const float Threshold = 12f;

        // How long a pointer can stand still before the let go does not carry the content on (milliseconds).
        private const long HoldBeforeReleaseMs = 100;

        private readonly ScrollPhysics _physics = new ScrollPhysics();
        private IVisualElementScheduledItem _animation;

        private bool _pressed;
        private bool _dragging;
        private int _pointerId;
        private float _startY;
        private float _startPosition;
        private float _lastY;
        private long _lastTime;
        private float _velocity;

        private ScrollView Scroll => (ScrollView)target;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);

            // Runs only while the content is moving by itself (after a let go); paused the rest of the time.
            _animation = target.schedule.Execute(OnTick).Every(16);
            _animation.Pause();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            _animation?.Pause();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            // A finger is the ScrollView's own business. Only the left button (or the pen tip) drags.
            if (evt.pointerType == UnityEngine.UIElements.PointerType.touch || evt.button != (int)MouseButton.LeftMouse) return;

            // Holding the content stops it, wherever it was going; a content that is still pulled past an end is held from there.
            _animation.Pause();
            _physics.Max = Scroll.verticalScroller.highValue;
            _startPosition = _physics.IsMoving ? _physics.Offset - _physics.Overscroll : Scroll.scrollOffset.y;

            _pressed = true;
            _dragging = false;
            _pointerId = evt.pointerId;
            _startY = evt.position.y;
            _lastY = evt.position.y;
            _lastTime = evt.timestamp;
            _velocity = 0f;
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_pressed || evt.pointerId != _pointerId) return;

            float moved = evt.position.y - _startY;
            if (!_dragging)
            {
                if (Mathf.Abs(moved) < Threshold) return;

                _dragging = true;
                target.CapturePointer(_pointerId);
            }

            // Dragging down moves the content down, so the offset goes the other way.
            _physics.Max = Scroll.verticalScroller.highValue;
            _physics.Drag(_startPosition - moved);
            Apply();

            // The speed the pointer has now, a little smoothed, so the let go carries the content on at about that speed.
            if (evt.timestamp > _lastTime)
            {
                float seconds = (evt.timestamp - _lastTime) / 1000f;
                float speed = -(evt.position.y - _lastY) / seconds;
                _velocity = Mathf.Lerp(_velocity, speed, 0.5f);
            }
            _lastY = evt.position.y;
            _lastTime = evt.timestamp;
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_pressed || evt.pointerId != _pointerId) return;

            bool wasDragging = _dragging;
            _pressed = false;
            _dragging = false;

            if (wasDragging)
            {
                if (target.HasPointerCapture(_pointerId)) target.ReleasePointer(_pointerId);

                // A pointer that stopped before it let go leaves the content where it is.
                float speed = evt.timestamp - _lastTime > HoldBeforeReleaseMs ? 0f : _velocity;
                _physics.Release(speed);
                evt.StopPropagation();
            }

            // A tap on a content that was still pulled past an end lets it go back too.
            if (_physics.IsMoving) _animation.Resume();
        }

        private void OnCaptureOut(PointerCaptureOutEvent evt)
        {
            _pressed = false;
            _dragging = false;
        }

        private void OnTick(TimerState state)
        {
            _physics.Max = Scroll.verticalScroller.highValue;
            float dt = Mathf.Min(state.deltaTime / 1000f, 0.05f);
            bool moving = _physics.Step(dt);
            Apply();
            if (!moving) _animation.Pause();
        }

        // The offset scrolls the ScrollView; what is past an end is shown by moving the content (the ScrollView itself cannot go past).
        // The ScrollView scrolls by the content's translate, so this uses top, which it does not touch.
        private void Apply()
        {
            Scroll.scrollOffset = new Vector2(Scroll.scrollOffset.x, _physics.Offset);
            if (_physics.Overscroll == 0f) Scroll.contentContainer.style.top = StyleKeyword.Null;
            else Scroll.contentContainer.style.top = _physics.Overscroll;
        }
    }
}
