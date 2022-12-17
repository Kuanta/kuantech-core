using System;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public enum AudioTypes
    {
        None,
        CoinPickup,
    }
    
    [Serializable]
    public class AudioClipDictionary : SerializableDictionary<AudioTypes, Sound>
    {
    }

    public class AudioLibrary : MonoBehaviour
    {
        public AudioClipDictionary Audios;
        
        private void Awake()
        {
            foreach (var sound in Audios.Values)
            {
                sound.AudioSource = gameObject.AddComponent<AudioSource>();
                sound.AudioSource.clip = sound.Clip;
                sound.AudioSource.volume = sound.Volume;
                sound.AudioSource.pitch = sound.Pitch;
            }
        }

        public void PlaySound(AudioTypes audioType)
        {
            if (audioType == AudioTypes.None || Audios == null || !Audios.ContainsKey(audioType)) return;
            if( Audios[audioType].AudioSource.isPlaying) return;
            Audios[audioType].AudioSource.Play();
        }
    }
}