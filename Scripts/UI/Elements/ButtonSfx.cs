using Kuantech.Core.FX;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class ButtonSfx : MonoBehaviour
    {
        [SerializeField] private Sound Audio;
        [KTTag("AudioTag")]
        [SerializeField] private int AudioTag;
        [SerializeField] private Button Button;
        
        private void Awake()
        {
            Button ??= GetComponent<Button>();
            if (Button == null) return;
            Button.onClick.AddListener(ButtonPressHandler);
        }

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
    }
}