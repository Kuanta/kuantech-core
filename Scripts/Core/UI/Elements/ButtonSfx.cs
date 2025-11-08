using Kuantech.Core.FX;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Core.UI
{
    public class ButtonSfx : MonoBehaviour, KtButton.IUIButtonAction
    {
        [SerializeField] private Sound DefaultAudio;
        [KTTag("AudioTag")]
        [SerializeField] private int PositiveSound;
        [KTTag("AudioTag")]
        [SerializeField] private int NegativeSound;

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
            AudioLibrary.PlaySoundByTag(PositiveSound);
        }
        
        public void NegativeEffect()
        {
            AudioLibrary.PlaySoundByTag(NegativeSound);
        }
    }
}