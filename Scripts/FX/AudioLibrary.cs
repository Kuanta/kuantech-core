using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Kuantech.Core.FX
{
    public enum AudioTypes
    {
        None,
        CoinPickup,
        CloseButton,
        ClickSound,
    }
    
    [Serializable]
    public class AudioClipDictionary : SerializableDictionary<AudioTypes, Sound>
    {
    }

    public class AudioLibrary : MonoBehaviour
    {
        [Header("Adudio Mix")]
        [SerializeField] private AudioMixer MasterMixer;
        
        [SerializeField] private AudioMixerSnapshot Unpaused;
        
        public AudioClipDictionary Audios;
        
        [Header("Music")] 
        public AudioSource MainMenuMusic;
        public AudioSource IngameMusic;


        public void Initialize()
        {
            foreach (var sound in Audios.Values)
            {
                sound.AudioSource = gameObject.AddComponent<AudioSource>();
                sound.AudioSource.clip = sound.Clip;
                sound.AudioSource.volume = sound.Volume;
                sound.AudioSource.pitch = sound.Pitch;
            }
            
            //Load music values
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultValue:1f);
            float sfxVolume = PlayerPrefs.GetFloat("SfxVolume", defaultValue: 1f);
            SetMusicVolume(musicVolume);
            SetSfxVolume(sfxVolume);
        }
        
        public void PlaySound(AudioTypes audioType)
        {
            if (audioType == AudioTypes.None || Audios == null || !Audios.ContainsKey(audioType)) return;
            if( Audios[audioType].AudioSource.isPlaying) return;
            Audios[audioType].AudioSource.Play();
        }
        
        /// <summary>
        /// Sets the music volume
        /// </summary>
        /// <param name="value">Normalized volume value</param>
        public void SetMusicVolume(float value)
        {
            MasterMixer.SetFloat("musicVolume", Mathf.Log10(value * 0.5f) * 20);
        }

        public void SetSfxVolume(float value)
        {
            MasterMixer.SetFloat("sfxVolume", Mathf.Log10(value * 0.5f) * 20);
        }
    }
}