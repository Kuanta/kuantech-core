using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.UI
{
    public class UICanvas : MonoBehaviour {
        
        protected virtual void Start()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        public float TestZPos = 10;
        [SerializeField] private UnityEngine.Camera GameCamera;
        [SerializeField] private UnityEngine.Camera CanvasCamera;
        [SerializeField] private RectTransform RectTransform;
        [SerializeField] private Canvas Canvas;
        /// <summary>
        /// Flys a ui element to a target rect transform, starting from a world coordinate
        /// </summary>
        /// <param name="flyingElement"></param>
        /// <param name="target"></param>
        public void FlyUIElementFromWorldPosition(FlyingUIElement flyingElement, Vector3 worldPosition, RectTransform target, 
        object data = null, UnityAction TargetReachedHandler = null)
        {
            Vector3 screenPosition = GlobalToScreenPosition(worldPosition, GameCamera);
            flyingElement.transform.SetParent(transform);
            flyingElement.transform.localPosition = screenPosition;
            flyingElement.transform.localScale = Vector3.one;
            flyingElement.transform.localRotation = Quaternion.identity;
            Vector3 targetPos = target.position;
            Vector3 screenTargetPosition = GlobalToScreenPosition(targetPos, CanvasCamera);
            flyingElement.Fly(screenPosition, screenTargetPosition, data, TargetReachedHandler);
        }

        public void FlyTowardsUIElementInWorldSpace(FlyingUIElement flyingElement, Vector3 startPosition, RectTransform target, object data = null, UnityAction TargetReachedHandler = null )
        {
            Vector2 targetScreen = RectCenterToScreen(target);
            var ray = GameCamera.ScreenPointToRay(targetScreen);
            float depth = Vector3.Dot(startPosition - GameCamera.transform.position, GameCamera.transform.forward);
            if (depth < 0.01f) depth = 0.01f; // kamera ile aynı noktada olmasın
            Vector3 planePoint = GameCamera.transform.position + GameCamera.transform.forward * depth;
            var plane = new Plane(GameCamera.transform.forward, planePoint);

            Vector3 worldEnd = plane.Raycast(ray, out var enter)
                ? ray.GetPoint(enter)
                : (startPosition + GameCamera.transform.forward * 0.5f);
            flyingElement.Fly(startPosition, worldEnd, data, TargetReachedHandler);
        }
        #region Cameras

        public UnityEngine.Camera GetGameCamera()
        {
            return GameCamera;
        }

        public UnityEngine.Camera GetCanvasCamera()
        {
            return CanvasCamera;
        }
        #endregion

        #region Utility Methods
        public Vector2 GlobalToScreenPosition(Vector3 worldPosition, UnityEngine.Camera cam)
        {
            UnityEngine.Camera mainCamera = GameCamera != null ? GameCamera : UnityEngine.Camera.main;
            UnityEngine.Camera canvasCamera = CanvasCamera != null ? CanvasCamera : mainCamera;

            // Transform world position to screen point using the main camera
            Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);
            
            //For overlay camera
            if (Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return new Vector2(screenPos.x, screenPos.y);
            }
            
            // If the canvas camera is orthographic, adjust the screen position's depth
            
            // Convert screen point to local point in the canvas' RectTransform
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTransform, screenPos, canvasCamera, out localPoint);
            // if (canvasCamera.orthographic)
            // {
            //     localPoint.y = RectTransform.sizeDelta.y - localPoint.y; // Set the depth to near clip plane of the main camera
            // }
            return localPoint;
        }
        
        public Vector2 ScreenPositionToAnchoredPosition(RectTransform rectTransform, Vector2 screenPos)
        {
            if (rectTransform == RectTransform) return screenPos;
            RectTransform uiElementParent = rectTransform.parent as RectTransform;
            if (uiElementParent == null) return screenPos;
            Vector2 localPoint;
            if (Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(uiElementParent, screenPos, null,
                    out localPoint);
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(uiElementParent, screenPos, CanvasCamera,
                    out localPoint);
            }

            return localPoint;
        }

        public Vector3 RectCenterToScreen(RectTransform rt)
        {
            var eventCam =
                Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null :
                    (GetCanvasCamera() ? GetCanvasCamera() : Canvas.worldCamera);

            Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
            return RectTransformUtility.WorldToScreenPoint(eventCam, worldCenter);
        }
        #endregion

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Close()
        {
            gameObject.SetActive(false);
        }
    }
}