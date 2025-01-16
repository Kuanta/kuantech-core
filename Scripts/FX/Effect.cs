using System;
using System.Collections;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public class Effect : MonoBehaviour
    {
        [Header("Effect Properties")]
        public string EffectId;
        public float Duration;
        public float Delay = 0f;

        [Header("Visual Effect")]
        public VisualEffect Vfx;

        [Header("Sound Effect")]
        [KTTag("AudioTag")]
        public int AudioTag;
        public Sound Sfx;
        public float SfxFadeOutDuration = 0; //If set to a value >0, sfx will top with fading out

        [Header("Animations")]
        public Animator Animator;

        [NonSerialized] public bool ShouldBeRemoved = false; //This is used for queued audios

        private static readonly int Play1 = Animator.StringToHash("Play");

        //If an effect is under the protection of effects library, it can't be destroyed with timed calls
        [NonSerialized] public bool BoundToEffectsLibrary = false; 
        
        /// <summary>
        /// To simply play
        /// </summary>
        public void Play()
        {
            EffectPlaySettings.GetDefaultSettings();
            Play(EffectPlaySettings.GetDefaultSettings());
        }
        
        /// <summary>
        /// Plays the effect using the settings
        /// </summary>
        /// <param name="settings"></param>
        public void Play(EffectPlaySettings settings)
        {
            if (settings.SetPosition)
            {
                if (settings.EffectParent != null)
                {
                    transform.SetParent(settings.EffectParent);
                    transform.localPosition = settings.LocalPlayPosition;
                    transform.localRotation = settings.LocalPlayRotation;
                }
                else
                {
                    transform.position = settings.PlayPosition;
                    transform.rotation = settings.PlayRotation;
                }
            }

            _Play(settings);
            if (settings.DespawnAfterPlay && !BoundToEffectsLibrary)
            {
                StartCoroutine(PoolRoutine(Duration));
            }
        }

        
        public void _Play(EffectPlaySettings playSettings)
        {
            StartCoroutine(PlayRoutine(playSettings));
        }

        private IEnumerator PlayRoutine(EffectPlaySettings playSettings)
        {
            yield return new WaitForSeconds(Delay);
            PlayEffects(playSettings);
        }

        protected virtual void PlayEffects(EffectPlaySettings playSettings)
        {
            ShouldBeRemoved = false;
            if(Sfx != null)
            {
                Sfx.OnDeqeued = OnSoundDequeued;
            }

            //Sound
            if (!EffectsLibrary.CanPlayEffect(EffectId, playSettings.EffectCooldown)) return;

            if(!EffectsLibrary.PlayAudio(AudioTag))
            {
                if (Sfx != null)
                {
                    Sfx.ComboFromEffect = playSettings.ComboIndex;
                    Sfx.PlayThroughAudioLibrary();
                }
            }
            if (Vfx != null) Vfx.Play(playSettings);
            if(Animator != null) Animator.SetTrigger(Play1);
            EffectsLibrary.SetLastPlayedTime(EffectId);
        }

        #region Old Play Methods
        public void Play(Vector3 position, Quaternion rotation, float effectCooldown, bool local = false)
        {
            if (local)
            {
                transform.localPosition = position;
                transform.localRotation = rotation;
            }
            else
            {
                transform.position = position;
                transform.rotation = rotation;
            }
            //Play(effectCooldown);
        }

        public void Play(Transform parent, float effectCooldown)
        {
            Play(parent, Vector3.zero, Quaternion.identity, effectCooldown);
        }

        public void Play(Transform parent, Vector3 position, Quaternion rotation, float effectCooldown)
        {
            transform.SetParent(parent);
            Play(position, rotation,effectCooldown, local:true);
        }

        public void PlayTimed(float effectCooldown)
        {
            //Play(effectCooldown);
            StartCoroutine(PoolRoutine(Duration));
            
        }
        
        public void PlayTimed(float duration, Vector3 position, Quaternion rotation, float effectCooldown, bool local = false)
        {
            Play(position, rotation, effectCooldown, local);
            StartCoroutine(PoolRoutine(duration));
        }
                
        public void PlayTimed(float duration, Transform parent, float effectCooldown)
        {
            Play(parent, effectCooldown);
            StartCoroutine(PoolRoutine(duration));
        }
        
        public void PlayTimed(float duration, Transform parent, Vector3 position, Quaternion rotation, float effectCooldown)
        {
            Play(parent, position, rotation, effectCooldown);
            StartCoroutine(PoolRoutine(duration));
        }
        #endregion

        public void Stop()
        {
            if(Vfx!=null) Vfx.Stop();
            if (Sfx == null) return;
            Sfx.Stop(SfxFadeOutDuration);
       
        }
        public void SetAudioPitch(float pitch)
        {
            if (Sfx != null) Sfx.SetPitch(pitch);
        }
        private IEnumerator PoolRoutine(float duration)
        {
            if(BoundToEffectsLibrary) yield break;
            if(Sfx != null && Sfx.Enqueued)
            {
                ShouldBeRemoved = true;
                yield break;
            }
            if (duration < 0)
            {
                duration = Vfx.GetDuration();
            }
            yield return new WaitForSeconds(duration);
            
            if(Vfx != null) Vfx.Stop();
            if(Sfx != null) Sfx.Stop();
            EffectsLibrary.GetContext<EffectsLibrary>().EffectsPool.PoolObject(gameObject);
        }

        public void OnSoundDequeued()
        {
            Sfx.Enqueued = false;
            StartCoroutine(PoolRoutine(Duration));
        }
    }
}