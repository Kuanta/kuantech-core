using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIDragSlot : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected Image IconImage;

        [SerializeField] public DraggableSlotGhost GhostPrefab;
        private CanvasGroup _canvasGroup;
        private bool _dragStarted;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public virtual Sprite GetDragIcon() => IconImage != null ? IconImage.sprite : null;

        // ── Tap ───────────────────────────────────────────────────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            _dragStarted = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_dragStarted)
                UIDragDropManager.NotifySlotTapped(this);
        }

        // ── Drag source ───────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag()) return;
            _dragStarted = true;
            _canvasGroup.blocksRaycasts = false;
            UIDragDropManager.BeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UIDragDropManager.UpdateDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
            UIDragDropManager.EndDrag(this, eventData);
        }

        protected virtual bool CanDrag() => true;

        // ── Drop target ───────────────────────────────────────────────────────

        /// <summary>Returns true if this slot can accept the dragging slot. Cast source to your subtype.</summary>
        public virtual bool CanAcceptDrop(UIDragSlot source) => false;

        /// <summary>Called on the TARGET when a drop is accepted. Cast source to your subtype to read data.</summary>
        public virtual void OnDropReceived(UIDragSlot source) { }

        // ── Notifications ─────────────────────────────────────────────────────

        /// <summary>Called on the SOURCE when a target accepted the drop.</summary>
        public virtual void OnDataAccepted(UIDragSlot target) { }

        /// <summary>Called on the SOURCE when drag ended without hitting any valid target.</summary>
        public virtual void OnDragCancelled() { }

        // ── Hover ─────────────────────────────────────────────────────────────

        public virtual void OnPointerEnter(PointerEventData eventData) { }
        public virtual void OnPointerExit(PointerEventData eventData) { }
    }
}
