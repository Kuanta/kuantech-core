using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.UI
{
    public class UICanvas : MonoBehaviour {
        
        protected virtual void Start()
        {
            RectTransform = GetComponent<RectTransform>();
        }
        [SerializeField] private RectTransform RectTransform;
        /// <summary>
        /// Flys a ui element to a target rect transform, starting from a world coordinate
        /// </summary>
        /// <param name="flyingElement"></param>
        /// <param name="target"></param>
        public void FlyUIElementFromWorldPosition(FlyingUIElement flyingElement, Vector3 worldPosition, RectTransform target, 
        object data = null, UnityAction TargetReachedHandler = null)
        {
            flyingElement.Fly(GlobalToScreenPosition(worldPosition), target.position, data, TargetReachedHandler);
        }   

        public void ShowFloatingText(FloatingText floatingText, Vector3 worldPosition, Vector3 initialSpeed)
        {
            floatingText.transform.SetParent(transform);
            floatingText.transform.position = GlobalToScreenPosition(worldPosition);
            floatingText.Fly(initialSpeed);
        }

        public Vector2 GlobalToScreenPosition(Vector3 worldPosition)
        {
            return RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);

        }
    }
}