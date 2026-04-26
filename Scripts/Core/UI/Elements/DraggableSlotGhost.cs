using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// Ghost that follows the cursor during a drag.
    /// Override OnBeginDrag to read data from the source slot (cast to your subtype).
    /// Base implementation shows only the icon.
    /// </summary>
    public class DraggableSlotGhost : MonoBehaviour
    {
        [SerializeField] protected Image IconImage;

        private RectTransform _rect;

        protected virtual void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (IconImage != null) IconImage.raycastTarget = false;
        }

        /// <summary>
        /// Called when a drag starts. Cast source to your slot subtype to read icon, count, etc.
        /// </summary>
        public virtual void OnBeginDrag(UIDragSlot source)
        {
            if (IconImage != null) IconImage.sprite = source.GetDragIcon();
            gameObject.SetActive(true);
        }

        public virtual void OnDrop()
        {
            gameObject.SetActive(false);
        }

        public void SetPosition(Vector2 anchoredPos)
        {
            _rect ??= GetComponent<RectTransform>();
            _rect.anchoredPosition = anchoredPos;
        }
    }
}