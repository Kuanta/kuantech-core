using UnityEngine;

namespace Kuantech.Utils.UI
{
    public class PanelSizer : MonoBehaviour
    {
        [Tooltip("Padding between elements")] public Vector2 Padding;
        [Tooltip("Margin between elements and edges")] public Vector2 Margin;
        [Tooltip("Size of the elements")]public Vector2 ElementSizes;
        
        [SerializeField] private RectTransform RectTransform;
        
        public void SetHorizontalElementCount(int elementCount)
        {
            if (RectTransform == null)
            {
                RectTransform = GetComponent<RectTransform>();
            }

            float horizontalSize = Margin.x*2 + Mathf.Max(elementCount - 1, 0) * Padding.x + ElementSizes.x * elementCount;
            Vector2 currentSize = RectTransform.sizeDelta;
            currentSize.x = horizontalSize;
            RectTransform.sizeDelta = currentSize;
        }

        public void SetVerticalElementCount(int elementCount)
        {
            if (RectTransform == null)
            {
                RectTransform = GetComponent<RectTransform>();
            }

            float verticalSize = Margin.y*2 + Mathf.Max(elementCount - 1, 0) * Padding.y + ElementSizes.y * elementCount;
            Vector2 currentSize = RectTransform.sizeDelta;
            currentSize.y = verticalSize;
            RectTransform.sizeDelta = currentSize;
        }
    }
}