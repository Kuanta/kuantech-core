using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// Base drag-and-drop slot. Subclass to add data fields and override
    /// CanAcceptDrop / OnDropReceived / OnDataAccepted with direct casts.
    /// UIDragDropManager.DragSource always holds the active dragging slot.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIDragSlot : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected Image IconImage;

        // Set at runtime to override the default ghost for this slot type
        public DraggableSlotGhost GhostOverride;
        private CanvasGroup _canvasGroup;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>Returns the icon sprite used for the drag ghost. Override if icon is elsewhere.</summary>
        public virtual Sprite GetDragIcon() => IconImage != null ? IconImage.sprite : null;

        // ── Drag source ───────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag()) return;
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

        /// <summary>Override to prevent dragging under certain conditions (e.g. empty slot).</summary>
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
