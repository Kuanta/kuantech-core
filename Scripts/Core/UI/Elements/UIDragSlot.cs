using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public abstract class UIDragSlotData { }

    /// <summary>
    /// A drag-and-drop slot that can hold any UIDragSlotData.
    /// Subclass and override CanAcceptDrop / OnDropReceived to implement behavior.
    /// Also override RefreshVisual to update icon/text when data changes.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIDragSlot : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected Image IconImage;

        public UIDragSlotData Data { get; private set; }

        private CanvasGroup _canvasGroup;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        // ── Data ──────────────────────────────────────────────────────────────

        public virtual void SetData(UIDragSlotData data)
        {
            Data = data;
            RefreshVisual();
        }

        public virtual void ClearData()
        {
            Data = null;
            RefreshVisual();
        }

        /// <summary>Override to update icon, text, etc. when Data changes.</summary>
        protected virtual void RefreshVisual() { }

        /// <summary>Returns the icon sprite used for the drag ghost. Override if icon is elsewhere.</summary>
        public virtual Sprite GetDragIcon() => IconImage != null ? IconImage.sprite : null;

        // ── Drag source ───────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Data == null) return;
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

        // ── Drop target ───────────────────────────────────────────────────────

        public void OnDrop(PointerEventData eventData)
        {
            UIDragSlot source = UIDragDropManager.DragSource;
            if (source == null || source == this) return;
            if (!CanAcceptDrop(source.Data)) return;
            OnDropReceived(source, source.Data);
        }

        /// <summary>Returns true if this slot can accept the dragged data.</summary>
        public virtual bool CanAcceptDrop(UIDragSlotData data) => false;

        /// <summary>
        /// Called when a valid drop lands on this slot.
        /// sourceSlot is the slot the drag originated from.
        /// </summary>
        protected virtual void OnDropReceived(UIDragSlot sourceSlot, UIDragSlotData data) { }

        // ── Called by manager when drag ends without a valid target ───────────

        /// <summary>Called when the drag from this slot was cancelled (dropped on nothing).</summary>
        public virtual void OnDragCancelled() { }

        // ── Hover ─────────────────────────────────────────────────────────────

        public virtual void OnPointerEnter(PointerEventData eventData) { }
        public virtual void OnPointerExit(PointerEventData eventData) { }
    }
}
