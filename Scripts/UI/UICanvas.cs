using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.UI
{
    public class UICanvas : MonoBehaviour {
        
        protected virtual void Start()
        {
            RectTransform = GetComponent<RectTransform>();
        }
        [SerializeField] private Camera GameCamera;
        [SerializeField] private Camera CanvasCamera;
        [SerializeField] private RectTransform RectTransform;
        /// <summary>
        /// Flys a ui element to a target rect transform, starting from a world coordinate
        /// </summary>
        /// <param name="flyingElement"></param>
        /// <param name="target"></param>
        public void FlyUIElementFromWorldPosition(FlyingUIElement flyingElement, Vector3 worldPosition, RectTransform target, 
        object data = null, UnityAction TargetReachedHandler = null)
        {
            Vector3 screenPosition = GlobalToScreenPosition(worldPosition);
            flyingElement.transform.SetParent(transform);
            flyingElement.transform.localPosition = screenPosition;
            flyingElement.transform.localScale = Vector3.one;
            flyingElement.transform.localRotation = Quaternion.identity;
            Vector3 screenTargetPosition = GlobalToScreenPosition(target.position);
            flyingElement.Fly(screenPosition, screenTargetPosition, data, TargetReachedHandler);
        }   

        public void ShowFloatingText(FloatingText floatingText, Vector3 worldPosition, Vector3 initialSpeed)
        {
            floatingText.transform.SetParent(transform);
            floatingText.transform.localPosition = GlobalToScreenPosition(worldPosition);
            floatingText.transform.localScale = Vector3.one;
            floatingText.transform.localRotation = Quaternion.identity;
            floatingText.Fly(initialSpeed);
        }

        public Vector2 GlobalToScreenPosition(Vector3 worldPosition)
        {
            Camera camera = GameCamera != null ? GameCamera : Camera.main;
            Camera canvasCam = CanvasCamera != null ? CanvasCamera : camera;
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTransform, screenPos, canvasCam, out position);
            return position;
        }
    }
}