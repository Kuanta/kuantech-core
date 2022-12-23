using Kuantech.Core.FX;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class ButtonSfx : MonoBehaviour
    {
        [SerializeField] private AudioTypes AudioType;
        [SerializeField] private Button Button;
        
        private void Awake()
        {
            Button ??= GetComponent<Button>();
            if (Button == null) return;
            Button.onClick.AddListener(ButtonPressHandler);
        }

        private void ButtonPressHandler()
        {
            if (AudioType == AudioTypes.None) return;
            EffectsLibrary.Instance.AudioLibrary.PlaySound(AudioType);
        }
    }
}