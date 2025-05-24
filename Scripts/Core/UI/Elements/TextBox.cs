using TMPro;
using UnityEngine;

namespace Kuantech.Core.UI
{
    public class TextBox : MonoBehaviour
    {
        [SerializeField] private TMP_Text Text;

        public void SetText(string text)
        {
            Text.text = text;
        }
    }
}