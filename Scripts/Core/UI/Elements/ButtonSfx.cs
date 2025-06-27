using Kuantech.Core.FX;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.UI
{
    public class ButtonSfx : MonoBehaviour, KtButton.IUIButtonAction
    {
        [SerializeField] private Sound Audio;
        [KTTag("AudioTag")]
        [SerializeField] private int AudioTag;

        private void ButtonPressHandler()
        {
            if(Audio != null)
            {
                Audio.Play();
                return;
            }
            AudioLibrary audioLibrary = EffectsLibrary.GetAudioLibrary();
            if(audioLibrary != null) audioLibrary.PlaySound(AudioTag);
        }

        public void OnClick()
        {
            ButtonPressHandler();
        }
    }
}