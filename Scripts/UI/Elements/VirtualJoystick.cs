using Kuantech.Core.HyperCasual;
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
        }
        public Vector2 GetInputVector()
        {
            return _inputVector;
        }
    }

}
