using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Audio;

namespace Kuantech.Core.FX
{
    [Serializable]
    public struct AudioClipEntry
    {
        [KTTag("AudioClipTag")]
        public int ClipId;
        public AudioSource AudioSource;
    }
    public class AudioLibrary : MonoBehaviour
    {
        [Header("Audio Mix")]
        [SerializeField] private AudioMixer MasterMixer;
        
        [SerializeField] private AudioMixerSnapshot Unpaused;
        public List<AudioClipEntry> Clips;
        public Dictionary<int, AudioClipEntry> _audios;
        
        [Header("Music")] 
        public AudioSource MainMenuMusic;
        public AudioSource IngameMusic;

        public void Initialize()
        {
            //Load music values
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultValue:1f);
            float sfxVolume = PlayerPrefs.GetFloat("SfxVolume", defaultValue: 1f);
            int toggleMusic = PlayerPrefs.GetInt("ToggleMusic", defaultValue:1);
            int toggleSfx = PlayerPrefs.GetInt("ToggleSfx", defaultValue: 1);
            SetMusicVolume(toggleMusic == 1 ? musicVolume : 0.0001f);
            SetSfxVolume(toggleSfx == 1 ? sfxVolume : 0.0001f);

            _audios  = new Dictionary<int, AudioClipEntry>();
            foreach(var clip in Clips)
            {
                _audios[clip.ClipId] = clip;
            }
        }
        
        public void PlaySound(int audioType)
        {
            if (_audios == null || !_audios.ContainsKey(audioType)) return;
            if (_audios[audioType].AudioSource == null) return;
            _audios[audioType].AudioSource.Play();
        }
        
        /// <summary> 
        /// Sets the music volume
        /// </summary>
        /// <param name="value">Normalized volume value</param>
        public void SetMusicVolume(float value)
        {
            value = Mathf.Max(0.0001f, value);
            if (MasterMixer == null) return;
            MasterMixer.SetFloat("musicVolume", Mathf.Log10(value * 0.5f) * 20);
        }

        public void SetSfxVolume(float value)
        {
            value = Mathf.Max(0.0001f, value);
            if (MasterMixer == null) return;
            MasterMixer.SetFloat("sfxVolume", Mathf.Log10(value * 0.5f) * 20);
        }
    }
}