using System.Collections;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public class Effect : MonoBehaviour
    {
        public string EffectId;
        public AudioSource Sfx;
        public ParticleSystem Vfx;
        public float Duration;
        public float Delay = 0f;
        public float SfxFadeOutDuration = 0; //If set to a value >0, sfx will top with fading out
        public Animator Animator;
        private static readonly int Play1 = Animator.StringToHash("Play");
        public bool EmitEffect = false; //If set to true, effect will be emitted instead of play
        public int EmitCount = 1;

        public List<AudioSource> SfxColleciton;
        
        public void Play(float effectCooldown = -1)
        {
            StartCoroutine(PlayRoutine(effectCooldown));
        }

        private IEnumerator PlayRoutine(float effectCooldown = -1)
        {
            yield return new WaitForSeconds(Delay);
            PlayEffects(effectCooldown);
        }

        protected virtual void PlayEffects(float effectCooldown)
        {
            if (!EffectsLibrary.CanPlaySound(EffectId, effectCooldown)) return;
            if (SfxColleciton != null && SfxColleciton.Count > 0)
            {
                Sfx = SfxColleciton.GetRandomElement();
            }
            if(Sfx != null) Sfx.Play();
            if(Vfx != null && !EmitEffect) Vfx.Play();
            else if (Vfx != null && EmitEffect)
            {
                Vfx.transform.position = transform.position;
                Vfx.transform.forward = transform.forward;
                Vfx.Emit(EmitCount);
            }
            if(Animator != null) Animator.SetTrigger(Play1);
            EffectsLibrary.SetLastPlayedTime(EffectId);
        }
        
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
            Play(effectCooldown);
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
            Play(effectCooldown);
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
        
        public void Stop()
        {
            if(Vfx!=null) Vfx.Stop();
            if (Sfx == null) return;
            if (SfxFadeOutDuration > 0)
            {
                StartCoroutine(FadeOutCoroutine(Sfx, SfxFadeOutDuration));
            }
            else
            {
                Sfx.Stop();
            }
        }

        public void SetAudioPitch(float pitch)
        {
            if (Sfx != null) Sfx.pitch = pitch;
        }
        private IEnumerator PoolRoutine(float duration)
        {
            if (duration < 0)
            {
                duration = Vfx.main.duration;
            }
            yield return new WaitForSeconds(duration);
            
            if(Vfx != null) Vfx.Stop();
            if(Sfx != null) Sfx.Stop();
            EffectsLibrary.GetContext<EffectsLibrary>().EffectsPool.PoolObject(gameObject);
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