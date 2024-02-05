using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    [Serializable]
    public class Sound : MonoBehaviour
    {
        [KTTag("AudioTag")]
        public int AudioTag;

        [Tooltip("If not null, this will be played")]
        public List<AudioSource> SfxColleciton;
        public AudioSource AudioSource;
        public float Cooldown = 0.1f;
        
        public void Play()
        {
            if(!SfxColleciton.IsNullOrEmpty())
            {
                AudioSource = SfxColleciton.GetRandomElement();
            }
            if(AudioSource == null) return;
            AudioSource.Play();
        }

        public void PlayThroughAudioLibrary()
        {
            EffectsLibrary.PlaySound(this);
        }

        public void Stop(float fadeOutDuraiton=0f)
        {
            //todo(sfx): Implement fadeout
            if(AudioSource == null) return;
            AudioSource.Stop();
        }


        public void SetPitch(float pitch)
        {
            AudioSource.pitch = pitch;
        }

        #region FadeInOut

        private IEnumerator FadeOutCoroutine(AudioSource audioSource, float fadeOutSecs)
        {
            float startVolume = audioSource.volume;

            while (audioSource.volume > 0)
            {
                audioSource.volume -= startVolume * Time.deltaTime / fadeOutSecs;
                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = startVolume;
        }

        #endregion
    }
}