using Kuantech.Core.FX;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Core.UI
{
    public class ButtonSfx : MonoBehaviour, KtButton.IUIButtonAction
    {
        [SerializeField] private Sound DefaultAudio;
        [SerializeField] private Sound PositiveSound;
        [SerializeField] private Sound NegativeSound;

        private void ButtonPressHandler()
        {
            if(DefaultAudio != null)
            {
                DefaultAudio.Play();
                return;
            }
        }

        public void OnClick()
        {
            ButtonPressHandler();
        }
        
        public void PositiveEffect()
        {
            // Optional: Implement positive effect sound
            if (PositiveSound != null)
            {
                PositiveSound.Play();
            }
        }
        
        public void NegativeEffect()
        {
            // Optional: Implement negative effect sound
            if (NegativeSound != null)
            {
                NegativeSound.Play();
            }
        }
    }
}