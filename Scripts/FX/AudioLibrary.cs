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
        CoinEarnedSound, //For selling and getting coin from enemies
        WinStinger,
        LoseStinger,
        PositiveEffect,
        NegativeEffect,
        FireDamageEffect,
        ItemPickupSound,
        EquipItemSound,
        UnequipItemSound,
        UpgradeItemSound,
    }
    
    [Serializable]
    public class AudioClipDictionary : SerializableDictionary<AudioTypes, AudioSource>
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
            // foreach (var sound in Audios.Values)
            // {
            //     sound.AudioSource = gameObject.AddComponent<AudioSource>();
            //     sound.AudioSource.clip = sound.Clip;
            //     sound.AudioSource.volume = sound.Volume;
            //     sound.AudioSource.pitch = sound.Pitch;
            // }
            
            //Load music values
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultValue:1f);
            float sfxVolume = PlayerPrefs.GetFloat("SfxVolume", defaultValue: 1f);
            SetMusicVolume(musicVolume);
            SetSfxVolume(sfxVolume);
        }
        
        public void PlaySound(AudioTypes audioType)
        {
            if (audioType == AudioTypes.None || Audios == null || !Audios.ContainsKey(audioType)) return;
            if(Audios[audioType] == null || Audios[audioType].isPlaying) return;
            Audios[audioType].Play();
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