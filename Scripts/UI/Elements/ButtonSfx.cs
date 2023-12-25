using Kuantech.Core.FX;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class ButtonSfx : MonoBehaviour
    {
        [KTTag("AudioClipTag")]
        [SerializeField] private int AudioType;
        [SerializeField] private Button Button;
        
        private void Awake()
        {
            Button ??= GetComponent<Button>();
            if (Button == null) return;
            Button.onClick.AddListener(ButtonPressHandler);
        }

        private void ButtonPressHandler()
        {
            EffectsLibrary.Instance.AudioLibrary.PlaySound(AudioType);
        }
    }
}