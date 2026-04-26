using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// Singleton that lives on the Canvas. Manages the ghost image that follows the cursor
    /// during a drag operation. UIDragSlot talks to this.
    /// Place this on your root Canvas (or a persistent UI GameObject).
    /// </summary>
    public class UIDragDropManager : MonoBehaviour
    {
        [SerializeField] private Canvas _rootCanvas;
        [SerializeField] private Image _ghostImage;  // Full-screen transparent RectTransform with Image child

        private static UIDragDropManager _instance;

        /// <summary>The slot that is currently being dragged from. Null when no drag is active.</summary>
        public static UIDragSlot DragSource { get; private set; }

        private RectTransform _ghostRect;

        private void Awake()
        {
            _instance = this;
            _ghostRect = _ghostImage.GetComponent<RectTransform>();
            _ghostImage.raycastTarget = false;
            _ghostImage.gameObject.SetActive(false);
        }

        // ── Called by UIDragSlot ──────────────────────────────────────────────

        public static void BeginDrag(UIDragSlot source, PointerEventData eventData)
        {
            if (_instance == null) return;
            DragSource = source;

            Sprite icon = source.GetDragIcon();
            if (icon == null) return;

            _instance._ghostImage.sprite = icon;
            _instance._ghostImage.gameObject.SetActive(true);
            _instance.MoveGhost(eventData.position);
        }

        public static void UpdateDrag(PointerEventData eventData)
        {
            if (_instance == null || DragSource == null) return;
            _instance.MoveGhost(eventData.position);
        }

        public static void EndDrag(UIDragSlot source, PointerEventData eventData)
        {
            if (_instance == null) return;
            _instance._ghostImage.gameObject.SetActive(false);

            // If nothing received the drop, notify the source
            if (eventData.pointerEnter == null ||
                eventData.pointerEnter.GetComponentInParent<UIDragSlot>() == null)
            {
                source.OnDragCancelled();
            }

            DragSource = null;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void MoveGhost(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.transform as RectTransform,
                screenPos,
                _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
                out Vector2 localPos);

            _ghostRect.anchoredPosition = localPos;
        }
    }
}
