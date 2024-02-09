using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class TextWithBackground : MonoBehaviour {
        [SerializeField] private TMP_Text Text;
        [SerializeField] private RectTransform Background;
        [SerializeField] private Vector2 Padding;

        public void SetText(string text)
        {
            Text.text = text;
            RectTransform textRectTransform = Text.GetComponent<RectTransform>();
            float preferredWidth = LayoutUtility.GetPreferredWidth(textRectTransform);
            Background.sizeDelta = new Vector2(preferredWidth + Padding.x, Background.sizeDelta.y);
        }        
    }
}