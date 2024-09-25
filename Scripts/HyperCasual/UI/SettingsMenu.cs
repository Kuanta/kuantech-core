using System;
using Kuantech.Core.FX;
using Kuantech.UI;
using Kuantech.Utils.Mobile;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual
{
    public class SettingsMenu : UIMenu
    {
        [SerializeField] private Slider MusicVolume;
        [SerializeField] private Slider SfxVolume;
        [SerializeField] private ToggleButton ToggleMusicButton;
        [SerializeField] private ToggleButton ToggleSfxButton;
        [FormerlySerializedAs("ToggleHaptics")] [SerializeField] private ToggleButton ToggleHapticsButton;
        
        private void Awake()
        {
            CloseButton.onClick.AddListener(Close);
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultValue:1f);
            float sfxVolume = PlayerPrefs.GetFloat("SfxVolume", defaultValue: 1f);
            int toggleMusic = PlayerPrefs.GetInt("ToggleMusic", defaultValue:1);
            int toggleSfx = PlayerPrefs.GetInt("ToggleSfx", defaultValue: 1);
            if(SfxVolume != null) SfxVolume.onValueChanged.AddListener(OnSfxVolumeChange);
            if(MusicVolume != null) MusicVolume.onValueChanged.AddListener(OnMusicVolumeChange);
            
            if(ToggleSfxButton != null) ToggleSfxButton.SetState(Convert.ToBoolean(toggleSfx));
            if (ToggleMusicButton != null) ToggleMusicButton.SetState(Convert.ToBoolean(toggleMusic));
            bool toggleSfxState = ToggleSfxButton != null ? ToggleSfxButton.State : true;
            bool toggleMusicState = ToggleMusicButton != null ? ToggleMusicButton.State : true;
            if (SfxVolume != null) SfxVolume.value = toggleSfxState ? sfxVolume : 0.0001f;
            if (MusicVolume != null) MusicVolume.value = toggleMusicState ? musicVolume : 0.0001f;


            if (ToggleMusicButton != null) ToggleMusicButton.OnToggle += OnMusicToggle;
            if (ToggleSfxButton != null) ToggleSfxButton.OnToggle += OnSfxToggle;

            int toggleHaptics = PlayerPrefs.GetInt("Togglehaptics", defaultValue: 1);
            if (ToggleHapticsButton != null)
            {
                ToggleHapticsButton.SetState(Convert.ToBoolean(toggleHaptics));
                ToggleHapticsButton.OnToggle += OnHapticsToggle;
            }
            MobileToolsManager.ToggleHaptics(Convert.ToBoolean(toggleHaptics));
        }

        #region Sound

        private void OnMusicVolumeChange(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume",value);
            if(ToggleMusicButton != null)
            {
                value = ToggleMusicButton.State ? value : 0.0001f;
            }
            EffectsLibrary.GetContext<EffectsLibrary>().AudioLibrary.SetMusicVolume(value);
        }

        private void OnSfxVolumeChange(float value)
        {
            PlayerPrefs.SetFloat("SfxVolume", value);
            if(ToggleSfxButton != null)
            {
                value = ToggleSfxButton.State ? value : 0.0001f;
            }
            EffectsLibrary.GetContext<EffectsLibrary>().AudioLibrary.SetSfxVolume(value);
        }

        private void OnMusicToggle(bool toggle)
        {
            PlayerPrefs.SetInt("ToggleMusic", toggle ? 1 : 0 );
            float musicVol = MusicVolume != null ? MusicVolume.value : 1f;
            EffectsLibrary.GetContext<EffectsLibrary>().AudioLibrary.SetMusicVolume(toggle ? musicVol : 0.0001f);
        }

        private void OnSfxToggle(bool toggle)
        {
            PlayerPrefs.SetInt("ToggleSfx", toggle ? 1 : 0 );
            float sfxVol = MusicVolume != null ? SfxVolume.value : 1f;
            EffectsLibrary.GetContext<EffectsLibrary>().AudioLibrary.SetSfxVolume(toggle ? sfxVol : 0.0001f);
        }

        #endregion

        private void OnHapticsToggle(bool toggle)
        {
            MobileToolsManager.ToggleHaptics(toggle);
            PlayerPrefs.SetInt("ToggleHaptics", toggle ? 1:0);
        }
    }
}