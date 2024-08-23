using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Kuantech.Core.FX
{
    [Serializable]
    public class Sound : MonoBehaviour
    {
        public string AudioId;
        [KTTag("AudioTag")]
        public int AudioTag;

        [FormerlySerializedAs("SfxColleciton")] [Tooltip("If not null, this will be played")]
        public List<AudioSource> SfxCollection;
        public AudioSource AudioSource;
        public float Cooldown = 0.1f;
        public float ComboCooldown = 1f;

        [Tooltip("If is set to true, sounds will be queued")]
        public bool QueueSound = false;
        [NonSerialized] public bool Enqueued = false;

        [Tooltip("If set to true, AudioLibrary will be checked")]
        public bool PlayWithAudioLibrary;
        
        [Header("Pitch Adjustments")]
        public float BasePitch = 1f;
        public float MinPitch = 0f;
        public float MaxPitch = 1.5f;

        [Header("Pitch Randomization")]
        public bool RandomizePitch = false;
        public float PitchVariation = 0.0f;

        [Header("ChangingPitch")]
        public bool AdjustPitch = false;
        public float PitchAdjustmentPerPlay = 0.1f;
        public float PitchResetTime;

        public UnityAction OnDeqeued;
        public void Play()
        {
            if (PlayWithAudioLibrary)
            {
                EffectsLibrary.PlayAudio(AudioTag);
                return;
            }
            if (!SfxCollection.IsNullOrEmpty())
            {
                AudioSource = SfxCollection.GetRandomElement();
            }
            if(AudioSource == null) return;
            
            float pitch = AudioSource.pitch;
            if(RandomizePitch)
            {
                pitch = BasePitch + UnityEngine.Random.Range(-1* PitchVariation, PitchVariation);
            }
            else if(AdjustPitch)
            {
                int comboCount = AudioLibrary.GetComboCount(this);
                pitch = BasePitch + comboCount * PitchAdjustmentPerPlay;

            }
            pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
            AudioSource.pitch = pitch;
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

        public void Deqeued()
        {
            OnDeqeued?.Invoke();
        }
    }
}