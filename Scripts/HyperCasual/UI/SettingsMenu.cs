using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual
{
    public class SettingsMenu : UIMenu
    {
        [SerializeField] private Button CloseButton;
        [SerializeField] private Slider MusicVolume;
        [SerializeField] private Slider SfxVolume;
        
        private void Awake()
        {
            CloseButton.onClick.AddListener(Close);
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultValue:1f);
            float sfxVolume = PlayerPrefs.GetFloat("SfxVolume", defaultValue: 1f);
            
            SfxVolume.onValueChanged.AddListener(OnSfxVolumeChange);
            MusicVolume.onValueChanged.AddListener(OnMusicVolumeChange);
            
            SfxVolume.value = sfxVolume;
            MusicVolume.value = musicVolume;
        }

        public override void Show()
        {
            base.Show();
            GameManager.Instance.PauseGame();
        }

        public override void Close()
        {
            base.Close();
            GameManager.Instance.ResumeGame();
        }

        private void OnMusicVolumeChange(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume",value);
            EffectsLibrary.Instance.AudioLibrary.SetMusicVolume(value);
        }

        private void OnSfxVolumeChange(float value)
        {
            PlayerPrefs.SetFloat("SfxVolume", value);
            EffectsLibrary.Instance.AudioLibrary.SetSfxVolume(value);
        }
    }
}