using Kuantech.Core.HyperCasual;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.UI;
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
        
        private bool _dragging;
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
            OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _inputVector = Vector2.zero;
            _targetInputVector = Vector2.zero;
            Dragging = false;
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
