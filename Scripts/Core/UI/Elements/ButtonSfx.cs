using Kuantech.Core.FX;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.UI
{
    public class ButtonSfx : MonoBehaviour, KtButton.IUIButtonAction
    {
        [KTTag("AudioTag")]
        [SerializeField] private int DefaultAudioaTag;
        [KTTag("AudioTag")]
        [SerializeField] private int PositiveSound;
        [KTTag("AudioTag")]
        [SerializeField] private int NegativeSound;

        private void ButtonPressHandler()
        {
            AudioLibrary.PlaySoundByTag(DefaultAudioaTag);
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