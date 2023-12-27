using System;
using Kuantech.Core.FX;
using Kuantech.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual
{
    public class SettingsMenu : UIMenu
    {
        [SerializeField] private Slider MusicVolume;
        [SerializeField] private Slider SfxVolume;
        [SerializeField] private ToggleButton ToggleMusicButton;
        [SerializeField] private ToggleButton ToggleSfxButton;
        
        private void Awake()
        {
            CloseButton.onClick.AddListener(Close);
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultValue:1f);
            float sfxVolume = PlayerPrefs.GetFloat("SfxVolume", defaultValue: 1f);
            int toggleMusic = PlayerPrefs.GetInt("ToggleMusic", defaultValue:1);
            int toggleSfx = PlayerPrefs.GetInt("ToggleSfx", defaultValue: 1);
            SfxVolume.onValueChanged.AddListener(OnSfxVolumeChange);
            MusicVolume.onValueChanged.AddListener(OnMusicVolumeChange);
            
            ToggleSfxButton.SetState(Convert.ToBoolean(toggleSfx));
            ToggleMusicButton.SetState(Convert.ToBoolean(toggleMusic));
            SfxVolume.value = ToggleSfxButton.State ? sfxVolume : 0.0001f;
            MusicVolume.value = ToggleMusicButton.State ? musicVolume : 0.0001f;
      

            ToggleMusicButton.OnToggle += OnMusicToggle;
            ToggleSfxButton.OnToggle += OnSfxToggle;
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
            value = ToggleMusicButton.State ? value : 0.0001f;
            EffectsLibrary.GetContext<EffectsLibrary>().AudioLibrary.SetMusicVolume(value);
        }

        private void OnSfxVolumeChange(float value)
        {
            PlayerPrefs.SetFloat("SfxVolume", value);
            value = ToggleSfxButton.State ? value : 0.0001f;
            EffectsLibrary.GetContext<EffectsLibrary>().AudioLibrary.SetSfxVolume(value);
        }

        private void OnMusicToggle(bool toggle)
        {
            PlayerPrefs.SetInt("ToggleMusic", toggle ? 1 : 0 );
            EffectsLibrary.GetContext<EffectsLibrary>().AudioLibrary.SetMusicVolume(toggle ? MusicVolume.value : 0.0001f);
        }

        private void OnSfxToggle(bool toggle)
        {
            PlayerPrefs.SetInt("ToggleSfx", toggle ? 1 : 0 );
            EffectsLibrary.GetContext<EffectsLibrary>().AudioLibrary.SetSfxVolume(toggle ? SfxVolume.value : 0.0001f);
        }
    }
}