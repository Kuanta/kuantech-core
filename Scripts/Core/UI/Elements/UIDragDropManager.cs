using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// SubManager that manages the ghost element following the cursor during UI drag operations.
    /// Access via GetContext&lt;UIDragDropManager&gt;() or the static helper methods called by UIDragSlot.
    /// </summary>
    public class UIDragDropManager : SubManager
    {
        [SerializeField] private Canvas _rootCanvas;
        [SerializeField] private DraggableSlotGhost _ghost;
        [SerializeField] private LayerMask _slotLayer;

        public static UIDragSlot DragSource { get; private set; }

        private UIDragSlot _lastHoveredSlot;
        private DraggableSlotGhost _activeGhost;

        private void Awake()
        {
            _ghost.gameObject.SetActive(false);
        }

        // ── Called by UIDragSlot ──────────────────────────────────────────────

        public static void BeginDrag(UIDragSlot source, PointerEventData eventData)
        {
            var ctx = GetContext<UIDragDropManager>();
            if (ctx == null) return;
            DragSource = source;
            ctx._activeGhost = source.GhostOverride != null ? source.GhostOverride : ctx._ghost;
            ctx._activeGhost.OnBeginDrag(source);
            ctx.MoveActiveGhost(eventData.position);
        }

        public static void UpdateDrag(PointerEventData eventData)
        {
            var ctx = GetContext<UIDragDropManager>();
            if (ctx == null || DragSource == null) return;
            ctx.MoveActiveGhost(eventData.position);
            ctx.UpdateHover(eventData.position);
        }

        public static void EndDrag(UIDragSlot source, PointerEventData eventData)
        {
            var ctx = GetContext<UIDragDropManager>();
            if (ctx == null) return;
            ctx.ClearHover();
            ctx._activeGhost.OnDrop();

            UIDragSlot target = ctx.RaycastForSlot(eventData.position);

            if (target != null && target != source && target.CanAcceptDrop(source))
            {
                target.OnDropReceived(source);
                source.OnDataAccepted(target);
            }
            else
            {
                source.OnDragCancelled();
            }

            DragSource = null;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void UpdateHover(Vector2 screenPos)
        {
            UIDragSlot hovered = RaycastForSlot(screenPos);
            if (hovered == _lastHoveredSlot) return;
            if (_lastHoveredSlot is IDragHoverable prev) prev.OnDragHoverExit();
            if (hovered is IDragHoverable next) next.OnDragHoverEnter(DragSource);
            _lastHoveredSlot = hovered;
        }

        private void ClearHover()
        {
            if (_lastHoveredSlot is IDragHoverable h) h.OnDragHoverExit();
            _lastHoveredSlot = null;
        }

        private UIDragSlot RaycastForSlot(Vector2 screenPos)
        {
            var pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                if ((_slotLayer & (1 << result.gameObject.layer)) == 0) continue;
                return result.gameObject.GetComponentInParent<UIDragSlot>();
            }
            return null;
        }

        private void MoveActiveGhost(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.transform as RectTransform,
                screenPos,
                _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
                out Vector2 localPos);

            _activeGhost.SetPosition(localPos);
        }
    }
}
