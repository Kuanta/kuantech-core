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
        
        [Header("Displacement")] [SerializeField]
        private float DeltaSmoothTime = 0.1f;
        private float _deltaX;
        private float _deltaY;
        private float _deltaXSpeed;
        private float _deltaYSpeed;

        [Header("Motions")] 
        [SerializeField] private float TapTime = 0.1f;
        [SerializeField] private float SwipeTime = 0.1f;
        [SerializeField] private float SwipeThreshold = 100f;

        //Events
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
            _inputVector = Vector2.zero;
            _targetInputVector = Vector2.zero;
            _lastTapTime = Time.time;
            OnDrag(eventData);
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

        public void OnDrag(PointerEventData eventData)
        {
            Dragging = true;
            
            Vector2 position = eventData.position;
            Vector2 center = _startPosition;
            Vector2 direction = (position - center).normalized;
            float distance = Vector2.Distance(center, position);

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

        private void Update()
        {
            if (!Dragging)
            {
                _inputVector = Vector2.zero;
                _targetInputVector = Vector2.zero;
            }
            _inputVector = Vector2.Lerp(_inputVector, _targetInputVector, Time.deltaTime * LerpFactor);
            
            _deltaX = Mathf.SmoothDamp(_deltaX, Input.GetAxis("Mouse X"), ref _deltaXSpeed, DeltaSmoothTime);
            _deltaY = Mathf.SmoothDamp(_deltaY, Input.GetAxis("Mouse Y"), ref _deltaYSpeed, DeltaSmoothTime);
        }
        public Vector2 GetInputVector()
        {
            return _inputVector;
        }
        public float GetHorizontalDisplacement()
        {
            if (!_dragging) return 0f;
            return _deltaX;
        }
        
        public float GetVerticalDisplacement()
        {
            if (!_dragging) return 0f;
            return _deltaY;
        }
    }

}
