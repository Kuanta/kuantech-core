using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kuantech.Core.UI.Kuantech.Input
{
    // Enum to define the behavior of the joystick
    public enum JoystickType
    {
        Fixed,      // Stays in one position (e.g., Attack button, static movement stick)
        Floating,   // Jumps to touch position, then stays static
        Dynamic     // Jumps to touch position, and background follows finger if dragged too far
    }

    public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;

        [Header("Settings")]
        [SerializeField] private JoystickType joystickType = JoystickType.Floating; // Select behavior here
        [SerializeField] private float handleRange = 100f; // Max distance the handle can move
        [SerializeField] private bool hideOnRelease = true; // Should it disappear when not in use?

        [Header("Swipe Settings")]
        [SerializeField] private bool detectSwipe = true;       
        [SerializeField] private float swipeTimeLimit = 0.2f;   
        [SerializeField] private float swipeThreshold = 100f;   

        public event Action<Vector2> OnSwipeDetected;

        // The output vector (-1 to 1)
        public Vector2 Direction { get; private set; } = Vector2.zero;

        private Canvas _canvas;
        private UnityEngine.Camera _cam; // Used if canvas is Screen Space - Camera

        // Swipe & Touch Logic variables
        private Vector2 _touchStartPos;
        private float _touchStartTime;
        private bool _isSwiping = false;
        private Vector2 _fixedPosition; // To store original position for Fixed mode

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            _canvas = GetComponentInParent<Canvas>();

            if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera)
                _cam = _canvas.worldCamera;

            // Store the initial position for Fixed mode so we can reset or keep it there
            _fixedPosition = background.position;

            if (hideOnRelease)
            {
                background.gameObject.SetActive(false);
            }
            else
            {
                // If it's fixed and always visible, ensure it's active
                background.gameObject.SetActive(true);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _touchStartTime = Time.time;
            _touchStartPos = eventData.position;
            _isSwiping = false;

            // Handle Joystick Positioning based on Type
            if (joystickType != JoystickType.Fixed)
            {
                // For Floating and Dynamic, move background to touch point
                background.position = eventData.position;
                handle.anchoredPosition = Vector2.zero;
            }
            else
            {
                // For Fixed, ensure handle resets to center of the background
                // We do NOT move the background.position
                handle.anchoredPosition = Vector2.zero;
            }

            // Immediately calculate input (useful for instant reaction)
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // If swipe was detected, disable joystick logic
            if (_isSwiping)
            {
                background.gameObject.SetActive(false);
                Direction = Vector2.zero;
                return;
            }
            
            // --- SWIPE CHECK ---
            if (detectSwipe)
            {
                float timeElapsed = Time.time - _touchStartTime;
                Vector2 totalDelta = eventData.position - _touchStartPos;

                if (timeElapsed <= swipeTimeLimit)
                {
                    return;

                }
            }

            // --- JOYSTICK LOGIC ---

            // Ensure background is active (in case it was hidden)
            if (!background.gameObject.activeSelf)
                background.gameObject.SetActive(true);

            // 1. Calculate the raw vector from Background Center to Touch Position
            Vector2 position = Vector2.zero;
            
            // We use the background's position as the anchor
            Vector2 centerToTouch = eventData.position - (Vector2)background.position;

            // 2. Clamp the handle position
            Vector2 clampedPosition = Vector2.ClampMagnitude(centerToTouch, handleRange);

            // 3. Apply to Handle
            handle.anchoredPosition = clampedPosition;

            // 4. Calculate Output Direction (-1 to 1)
            Direction = clampedPosition / handleRange;

            // --- DYNAMIC ORIGIN LOGIC ---
            // If the user drags further than the range, move the background along
            if (joystickType == JoystickType.Dynamic)
            {
                if (centerToTouch.magnitude > handleRange)
                {
                    // Calculate the direction of the overflow
                    Vector2 moveDirection = centerToTouch.normalized;
                    
                    // Move the background so the handle remains at the edge (handleRange)
                    // New Background Pos = Current Touch Pos - (Direction * Radius)
                    Vector2 newBackgroundPos = eventData.position - (moveDirection * handleRange);
                    
                    background.position = newBackgroundPos;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Direction = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;
            _isSwiping = false;

            if (hideOnRelease)
            {
                background.gameObject.SetActive(false);
            }

            float dragTime = Time.time - _touchStartTime;
            Vector2 totalDelta = eventData.position - _touchStartPos;
            float totalTraversed = totalDelta.magnitude;
        
            Debug.Log("Drag Time: " + dragTime + ", Total Traversed: " + totalTraversed );
            if (detectSwipe && dragTime < swipeTimeLimit && totalTraversed >= swipeThreshold)
            {
                OnSwipeDetected?.Invoke(totalDelta.normalized);

            }
            // If it is Fixed and we moved it (which shouldn't happen in Fixed mode logic, but safe to check),
            // reset visual state. 
            // Note: Fixed joysticks usually stay visible, so check your 'hideOnRelease' setting in inspector.
        }
    }
}