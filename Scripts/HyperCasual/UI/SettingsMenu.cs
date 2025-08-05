using System;
using Kuantech.Core.FX;
using Kuantech.Core.UI;
using Kuantech.Utils.Mobile;
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
        [SerializeField] private ToggleButton ToggleHapticsButton;
 
        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();

            if(SfxVolume != null) SfxVolume.onValueChanged.AddListener(OnSfxVolumeChange);
            if(MusicVolume != null) MusicVolume.onValueChanged.AddListener(OnMusicVolumeChange);
            if (ToggleMusicButton != null) ToggleMusicButton.OnToggle += OnMusicToggle;
            if (ToggleSfxButton != null) ToggleSfxButton.OnToggle += OnSfxToggle;

            UpdateElements();

            if (ToggleHapticsButton != null)
            {
                ToggleHapticsButton.OnToggle += OnHapticsToggle;
            }
        }

        private void UpdateElements()
        {
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultValue:1f);
            float sfxVolume = PlayerPrefs.GetFloat("SfxVolume", defaultValue: 1f);
            int toggleMusic = PlayerPrefs.GetInt("ToggleMusic", defaultValue:1);
            int toggleSfx = PlayerPrefs.GetInt("ToggleSfx", defaultValue: 1);
            int toggleHaptics = PlayerPrefs.GetInt("ToggleHaptics", defaultValue: 1);
            
            if(ToggleSfxButton != null) ToggleSfxButton.SetState(Convert.ToBoolean(toggleSfx));
            if (ToggleMusicButton != null) ToggleMusicButton.SetState(Convert.ToBoolean(toggleMusic));
            if (SfxVolume != null) SfxVolume.value = toggleSfx > 0 ? sfxVolume : 0.0001f;
            if (MusicVolume != null) MusicVolume.value = toggleMusic > 0 ? musicVolume : 0.0001f;
            if (ToggleHapticsButton != null)
            {
                ToggleHapticsButton.SetState(Convert.ToBoolean(toggleHaptics));
            }

        }
        public override void Show()
        {
            base.Show();
            UpdateElements();
            GameManager.PauseGame();
        }

        public override void Hide()
        {
            base.Hide();
            GameManager.ResumeGame();
        }
        
        #region Sound

        private void OnMusicVolumeChange(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume",value);
            if(ToggleMusicButton != null)
            {
                value = ToggleMusicButton.State ? value : 0.0001f;
            }
            AudioLibrary al = EffectsLibrary.GetAudioLibrary();
            if (al == null) return;
            al.SetMusicVolume(value);
        }

        private void OnSfxVolumeChange(float value)
        {
            PlayerPrefs.SetFloat("SfxVolume", value);
            if(ToggleSfxButton != null)
            {
                value = ToggleSfxButton.State ? value : 0.0001f;
            }

            AudioLibrary al = EffectsLibrary.GetAudioLibrary();
            if (al == null) return;
            al.SetSfxVolume(value);
        }

        private void OnMusicToggle(bool toggle)
        {
            PlayerPrefs.SetInt("ToggleMusic", toggle ? 1 : 0 );
            float musicVol = MusicVolume != null ? MusicVolume.value : 1f;
            AudioLibrary al = EffectsLibrary.GetAudioLibrary();
            if (al == null) return;
            al.SetMusicVolume(toggle ? musicVol : 0.0001f);
        }

        private void OnSfxToggle(bool toggle)
        {
            PlayerPrefs.SetInt("ToggleSfx", toggle ? 1 : 0 );
            float sfxVol = MusicVolume != null ? SfxVolume.value : 1f;
            AudioLibrary al = EffectsLibrary.GetAudioLibrary();
            if (al == null) return;
            al.SetSfxVolume(toggle ? sfxVol : 0.0001f);
        }

        #endregion

        private void OnHapticsToggle(bool toggle)
        {
            PlayerPrefs.SetInt("ToggleHaptics", toggle ? 1:0);
            MobileToolsManager.ToggleHaptics(toggle);
        }
    }
}