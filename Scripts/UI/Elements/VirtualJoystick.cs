using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Kuantech.Core.UI
{
    public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public Image bgImage;
        public Image joystickImage;

        private Vector3 inputVector;
        private Vector3 defaultPosition;

        private void Start()
        {
            defaultPosition = bgImage.transform.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 pos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bgImage.rectTransform, eventData.position, eventData.pressEventCamera, out pos))
            {
                pos.x = (pos.x / bgImage.rectTransform.sizeDelta.x);
                pos.y = (pos.y / bgImage.rectTransform.sizeDelta.y);

                inputVector = new Vector3(pos.x * 2, 0, pos.y * 2);
                inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

                joystickImage.rectTransform.anchoredPosition = new Vector3(inputVector.x * (bgImage.rectTransform.sizeDelta.x / 3), inputVector.z * (bgImage.rectTransform.sizeDelta.y / 3));
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            inputVector = Vector3.zero;
            joystickImage.rectTransform.anchoredPosition = Vector3.zero;
            bgImage.transform.position = defaultPosition;
        }

        public float GetHorizontalInput()
        {
            return inputVector.x;
        }

        public float GetVerticalInput()
        {
            return inputVector.z;
        }
    }

}
