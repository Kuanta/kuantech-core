using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// An on-screen analog stick with a background and a handle.
    ///
    /// Output is raw and unsmoothed: smoothing belongs to whatever consumes it (a movement module already
    /// eases toward its target), and smoothing here would only add a second lag the designer cannot see.
    ///
    /// Works in canvas space via RectTransformUtility rather than raw screen pixels, so the radius stays
    /// correct under canvas scaling and on any render mode. It reads only the event data it is given —
    /// never Input.mousePosition — so multiple touches (move stick + aim stick) do not fight each other.
    /// </summary>
    public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public enum JoystickMode
        {
            /// <summary>Stays where it is authored. The touch must start on it.</summary>
            Fixed,
            /// <summary>Hidden until touched, then appears under the finger and stays there.</summary>
            Floating,
            /// <summary>Like Floating, but follows the finger once it is dragged past the edge.</summary>
            Dynamic,
        }

        // Sticks live in the HUD while the code reading them lives on the player actor, so they publish
        // themselves by id rather than being wired through a serialized reference across that boundary.
        private static readonly System.Collections.Generic.Dictionary<string, TouchJoystick> Registry = new();

        /// <summary>Finds an active joystick by its id, or null if none is present (e.g. on desktop).</summary>
        public static TouchJoystick GetById(string joystickId)
        {
            if (string.IsNullOrEmpty(joystickId)) return null;
            Registry.TryGetValue(joystickId, out TouchJoystick joystick);
            return joystick;
        }

        [Header("Setup")]
        [Tooltip("Id other systems look this stick up by, e.g. \"Move\" or \"Aim\".")]
        [SerializeField] private string JoystickId;
        [Tooltip("Ring/base graphic. Its rect defines the stick radius, and its pivot must be centered.")]
        [SerializeField] private RectTransform Background;
        [Tooltip("Knob graphic. Moves inside the background.")]
        [SerializeField] private RectTransform Handle;

        [Header("Behaviour")]
        [SerializeField] private JoystickMode Mode = JoystickMode.Floating;
        [Tooltip("How far the handle travels, as a fraction of the background radius.")]
        [Range(0.1f, 1f)] [SerializeField] private float HandleRange = 0.8f;
        [Tooltip("Input below this fraction of the radius reads as zero, so a resting thumb does not drift.")]
        [Range(0f, 0.9f)] [SerializeField] private float DeadZone = 0.1f;
        [Tooltip("Hide the stick while untouched. Forced on for Floating and Dynamic.")]
        [SerializeField] private bool HideWhenIdle = true;

        /// <summary>Current input, magnitude 0..1. Zero while untouched.</summary>
        public Vector2 Direction { get; private set; }
        public float Horizontal => Direction.x;
        public float Vertical => Direction.y;
        public bool IsPressed { get; private set; }

        public UnityAction OnPressed;
        public UnityAction OnReleased;
        public UnityAction<Vector2> OnDirectionChanged;

        private Vector2 _backgroundHome;
        private CanvasGroup _canvasGroup;

        // Radius in canvas units. Read live so it stays right if the layout rescales the background.
        private float Radius => Background != null ? Background.rect.width * 0.5f : 0f;
        private bool AutoHides => HideWhenIdle || Mode != JoystickMode.Fixed;

        private void Awake()
        {
            if (Background == null || Handle == null)
            {
                Debug.LogError($"TouchJoystick ({name}): Background and Handle must both be assigned.");
                enabled = false;
                return;
            }

            _backgroundHome = Background.anchoredPosition;

            // A stretched background (anchorMin != anchorMax) has a size that follows the screen, so its
            // radius changes per aspect ratio — that is the iOS stretch. Give it a fixed size instead.
            if (Background.anchorMin != Background.anchorMax)
                Debug.LogWarning($"TouchJoystick ({name}): Background has stretched anchors. Anchor it to a " +
                                 "single point (e.g. center) with a fixed width/height, or it will resize per device.");

            // A CanvasGroup fades the whole stick without disabling it — disabling the object would drop
            // the pointer events mid-drag.
            _canvasGroup = Background.GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = Background.gameObject.AddComponent<CanvasGroup>();

            SetVisible(!AutoHides);
        }

        // Places the background under a screen point, correct for ANY anchor/pivot: convert the point into
        // the background's parent space, then subtract where the anchor sits so the result is a valid
        // anchoredPosition. Setting anchoredPosition from a point measured against a different origin is
        // what left the stick offset from the finger.
        private void MoveBackgroundUnder(Vector2 screenPos, UnityEngine.Camera cam)
        {
            RectTransform parent = Background.parent as RectTransform;
            if (parent == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, cam, out Vector2 local))
                return;

            Vector2 anchorCenter = (Background.anchorMin + Background.anchorMax) * 0.5f;
            Vector2 anchorOffset = (anchorCenter - parent.pivot) * parent.rect.size;
            Background.anchoredPosition = local - anchorOffset;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;

            // Floating and Dynamic drop the stick wherever the finger landed.
            if (Mode != JoystickMode.Fixed)
                MoveBackgroundUnder(eventData.position, eventData.pressEventCamera);

            SetVisible(true);
            OnPressed?.Invoke();
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            float radius = Radius;
            if (radius <= 0f) return;

            // Local point inside the background — with a centered pivot this is already the offset from
            // the stick's centre, in canvas units.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Background, eventData.position, eventData.pressEventCamera, out Vector2 offset))
                return;

            Vector2 raw = offset / radius;
            float magnitude = raw.magnitude;

            if (Mode == JoystickMode.Dynamic && magnitude > 1f)
            {
                // Drag the base along so the handle stays pinned at the edge instead of capping out.
                Background.anchoredPosition += raw.normalized * ((magnitude - 1f) * radius);
                magnitude = 1f;
            }

            Vector2 clamped = magnitude > 1f ? raw.normalized : raw;

            // Rescale past the dead zone so the very first responsive input is near zero rather than
            // jumping straight to DeadZone — otherwise the stick feels like it snaps on.
            float scaled = clamped.magnitude;
            Direction = scaled <= DeadZone
                ? Vector2.zero
                : clamped.normalized * ((scaled - DeadZone) / (1f - DeadZone));

            Handle.anchoredPosition = clamped * (radius * HandleRange);
            OnDirectionChanged?.Invoke(Direction);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressed = false;
            Direction = Vector2.zero;
            Handle.anchoredPosition = Vector2.zero;
            Background.anchoredPosition = _backgroundHome;

            SetVisible(!AutoHides);
            OnDirectionChanged?.Invoke(Direction);
            OnReleased?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = visible ? 1f : 0f;
        }

        /// <summary>Clears the stick — call when input is disabled mid-drag so it does not stay held.</summary>
        public void ResetJoystick()
        {
            IsPressed = false;
            Direction = Vector2.zero;
            if (Handle != null) Handle.anchoredPosition = Vector2.zero;
            if (Background != null) Background.anchoredPosition = _backgroundHome;
            SetVisible(!AutoHides);
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(JoystickId)) Registry[JoystickId] = this;
        }

        private void OnDisable()
        {
            // Only clear the slot if it is still ours — a replacement stick may already have claimed it.
            if (!string.IsNullOrEmpty(JoystickId) && Registry.TryGetValue(JoystickId, out TouchJoystick current) && current == this)
                Registry.Remove(JoystickId);

            ResetJoystick();
        }
    }
}
