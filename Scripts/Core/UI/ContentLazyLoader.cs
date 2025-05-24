using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.UI
{

    public class ContentLazyLoader : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private float horizontalBuffer = 200f;
        [SerializeField] private float verticalBuffer = 200f;

        private List<RectTransform> trackedItems = new();

        private void Start()
        {
            Canvas.ForceUpdateCanvases(); // Safe init
            ForceUpdateVisibility();
        }
        
        public void SetTrackedItems(List<RectTransform> items)
        {
            trackedItems = items;
            ForceUpdateVisibility();
        }

        public void AddItem(RectTransform item)
        {
            trackedItems.Add(item);
        }

        private void Update()
        {
            if (viewport == null || trackedItems == null) return;

            foreach (var item in trackedItems)
            {
                if (item == null) continue;

                bool visible = IsVisible(viewport, item, horizontalBuffer, verticalBuffer);
                item.gameObject.SetActive(visible);
            }
        }

        bool IsVisible(RectTransform outer, RectTransform inner, float horizontalPad, float verticalPad)
        {
            Vector3[] outerCorners = new Vector3[4];
            Vector3[] innerCorners = new Vector3[4];

            outer.GetWorldCorners(outerCorners);
            inner.GetWorldCorners(innerCorners);

            Rect outerRect = new Rect(outerCorners[0], outerCorners[2] - outerCorners[0]);

            // Expand the viewport rect with horizontal and vertical padding
            outerRect.xMin -= horizontalPad;
            outerRect.xMax += horizontalPad;
            outerRect.yMin -= verticalPad;
            outerRect.yMax += verticalPad;

            for (int i = 0; i < 4; i++)
            {
                if (outerRect.Contains(innerCorners[i]))
                {
                    return true; // En az bir köşe görünür alandaysa aktif et
                }
            }
            return false;
        }
        
        public void ForceUpdateVisibility()
        {
            Update();
        }
    }
}