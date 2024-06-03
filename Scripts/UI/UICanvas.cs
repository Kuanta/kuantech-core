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
            Vector3 targetPos = target.position;
            targetPos.z = 100;
            Vector3 screenTargetPosition = GlobalToScreenPosition(targetPos);
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
            Camera mainCamera = GameCamera != null ? GameCamera : Camera.main;
            Camera canvasCamera = CanvasCamera != null ? CanvasCamera : mainCamera;

            // Transform world position to screen point using the main camera
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
            // If the canvas camera is orthographic, adjust the screen position's depth
        
            // Convert screen point to local point in the canvas' RectTransform
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTransform, screenPos, canvasCamera, out localPoint);
            // if (canvasCamera.orthographic)
            // {
            //     localPoint.y = RectTransform.sizeDelta.y - localPoint.y; // Set the depth to near clip plane of the main camera
            // }
            Debug.LogError("Local Pos:"+localPoint);
            return localPoint;
        }
    }
}