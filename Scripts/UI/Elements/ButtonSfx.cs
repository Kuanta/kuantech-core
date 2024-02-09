using Kuantech.Core.FX;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class ButtonSfx : MonoBehaviour
    {
        [SerializeField] private Sound Audio;
        [SerializeField] private Button Button;
        
        private void Awake()
        {
            Button ??= GetComponent<Button>();
            if (Button == null) return;
            Button.onClick.AddListener(ButtonPressHandler);
        }

        private void ButtonPressHandler()
        {
            Audio.Play();
        }
    }
}