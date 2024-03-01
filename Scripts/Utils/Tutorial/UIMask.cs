using Kuantech.UI;
using UnityEngine;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// A class that handles masking for ui
    /// </summary>
    public class UIMask : MonoBehaviour {
        public UICanvas ParentUICanvas;
        public string Id;
        public void MaskTint(RectTransform tint)
        {
            tint.transform.SetParent(transform, false);
            tint.transform.position = Vector3.zero;
        }

        public void SetPosition(GameObject worldPosition)
        {
            if(ParentUICanvas == null)
            {
                return;
            }
            Vector2 position = ParentUICanvas.GlobalToScreenPosition(worldPosition.transform.position);
            RectTransform rectTransform = GetComponent<RectTransform>();
            rectTransform.position = ParentUICanvas.transform.TransformPoint(position);
        }
    }
 
}