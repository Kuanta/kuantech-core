using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kuantech.Core.UI
{
    public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public float MaxRadius = 100.0f;
        public float DeadZone = 0.1f;
        public float LerpFactor = 10f;

        private Vector2 _inputVector = Vector2.zero;
        private Vector2 _targetInputVector = Vector2.zero;
        private Vector2 _startPosition;
        private Vector2 _dragStartPosition;
        
        [Header("Displacement")] [SerializeField]
        private float DeltaSmoothTime = 0.1f;
        public float DisplacementFactor = 1f;
        [SerializeField] private float DisplacementThreshold = 0.01f;
        private float _deltaX;
        private float _deltaY;
        private float _deltaXSpeed;
        private float _deltaYSpeed;
        public float MaxDragDistancepercentage = 0.5f; // Maximum drag in pixels to equate to "full" input. Used for calculating _deltaX and _deltaY


        [Header("Motions")] 
        [SerializeField] private float TapTime = 0.1f;
        [SerializeField] private float SwipeTime = 0.1f;
        [SerializeField] private float SwipeThreshold = 100f;

        //Events
        public EventHandler OnPointerDownEvent;
        public EventHandler TapEvent;
        public EventHandler<Vector2> SwipeEvent;
        
        //Prrivate State Variables
        private bool _dragging;
        private float _lastTapTime;
        public bool Dragging
        {
            get { return _dragging;}
            private set { _dragging = value; }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _startPosition = eventData.position;
            _dragStartPosition = eventData.position;
            _inputVector = Vector2.zero;
            _targetInputVector = Vector2.zero;
            _lastTapTime = Time.time;
            _lastPosition = Input.mousePosition;
            OnDrag(eventData);
            OnPointerDownEvent?.Invoke(this, EventArgs.Empty);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _inputVector = Vector2.zero;
            _targetInputVector = Vector2.zero;
            Dragging = false;
            
            //Check motions
            Vector2 diffVector = eventData.position - _startPosition;
            float distanceTraveled = Vector2.Distance(_startPosition, eventData.position);
            float timeDiff = Time.time - _lastTapTime;
            if (timeDiff <= TapTime && distanceTraveled < SwipeThreshold)
            {
                TapEvent?.Invoke(this, EventArgs.Empty);
            }else if (timeDiff <= SwipeTime && distanceTraveled > SwipeThreshold)
            {
                SwipeEvent?.Invoke(this, diffVector);
            }
        }
        private Vector2 _currentCursorPos;
        public void OnDrag(PointerEventData eventData)
        {
            Dragging = true;
            
            Vector2 position = eventData.position;
            Vector2 center = _startPosition;
            Vector2 direction = (position - center).normalized;
            float distance = Vector2.Distance(center, position);
           _currentCursorPos = position;

            if (distance > MaxRadius)
            {
                //position = center + direction * MaxRadius;
                distance = MaxRadius;
            }

            if (distance < DeadZone)
            {
               _targetInputVector = Vector2.zero;
            }
            else
            {
               _targetInputVector = direction * ((distance - DeadZone) / (MaxRadius - DeadZone));
            }
        }

        private Vector2 _lastPosition;
        private void Update()
        {
            if (!Dragging)
            {
                _inputVector = Vector2.zero;
                _targetInputVector = Vector2.zero;
            }

            _inputVector = Vector2.Lerp(_inputVector, _targetInputVector, Time.deltaTime * LerpFactor);

            if (Dragging && (_lastPosition - _currentCursorPos).magnitude <= DisplacementThreshold)
            {
                _deltaX = 0;
                _deltaY = 0;
                _dragStartPosition = _lastPosition;
            }

            else if (Dragging)
            {
                // The regular drag behavior
                Vector2 dragDelta = (Vector2)Input.mousePosition - _dragStartPosition;
                float maxDragDistance = Screen.width * Mathf.Clamp01(MaxDragDistancepercentage);
                _deltaX = Mathf.Clamp(dragDelta.x / maxDragDistance, -1f, 1f);
                _deltaY = Mathf.Clamp(dragDelta.y / maxDragDistance, -1f, 1f);
                _lastPosition = _currentCursorPos; 

            }else{
                _deltaX = 0f;
                _deltaY = 0f;
            }
        }

        public Vector2 GetInputVector()
        {
            return _inputVector;
        }

        public float GetHorizontalDisplacement()
        {
            if (!_dragging) return 0f;
            if(Mathf.Abs(_deltaX) <= DisplacementThreshold) return 0f;
            return Math.Sign(_deltaX);
        }
        
        public float GetVerticalDisplacement()
        {
            if (!_dragging) return 0f;
            if(Mathf.Abs(_deltaY) <= DisplacementThreshold) return 0f;
            return Math.Sign(_deltaY);
        }

    }
}
