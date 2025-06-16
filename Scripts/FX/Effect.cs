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
        public float DespawnDelay; //Give a bit of time for fade outs

        [Header("Visual Effect")]
        public VisualEffect Vfx;

        [Header("Sound Effect")]
        [KTTag("AudioTag")]
        public int AudioTag;
        public Sound Sfx;
        public float SfxFadeOutDuration = 0; //If set to a value >0, sfx will top with fading out

        [Header("Animations")]
        public Animator Animator;

        [Header("Shader Effect")] 
        [Tooltip("If set to true, renderers will be detected")] public bool DetectAllRenderers = true;
        public ShaderEffect ShaderEffect;
        public string ShaderEffectId;
        [NonSerialized] public ShaderEffect PlayedShaderEffect;


        private static readonly int Play1 = Animator.StringToHash("Play");

        //If an effect is under the protection of effects library, it can't be destroyed with timed calls
        [NonSerialized] public bool SpawnedFromPool = false; //This is used to determine if the effect was spawned from the pool or not. 

        
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
            
            //If duration is set, pool it
            if (settings.DespawnAfterPlay && Duration > 0)
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
            
            //Visual Effect
            if (Vfx != null) Vfx.Play(playSettings);
            
            //Animation
            if(Animator != null) Animator.SetTrigger(Play1);
            
            //Shader Effect
            if (ShaderEffect != null)
            {
                PlayedShaderEffect = ShaderEffect;
                if (DetectAllRenderers && playSettings.EffectParent != null)
                {
                    ShaderEffect.DetectAllRenderers(playSettings.EffectParent.gameObject);
                }
                ShaderEffect.PlayShaderEffect();
            }
            else if (playSettings.EffectParent != null && !string.IsNullOrEmpty(ShaderEffectId) && playSettings.EffectParent.TryGetComponent<Actor>(out Actor actor))
            {
                EffectsModule em = actor.GetModule<EffectsModule>();
                if (em != null)
                {
                    ShaderEffect se = em.GetShaderEffect(ShaderEffectId);
                    if (se != null)
                    {
                        se.PlayShaderEffect();
                        PlayedShaderEffect = se;
                    }
                }
            }
            
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
            if (Sfx != null) Sfx.Stop(SfxFadeOutDuration);
            if (PlayedShaderEffect != null)
            {
                PlayedShaderEffect.StopShaderEffect();
            }
            if (SpawnedFromPool)
            {
                Despawn();
            }
        }
        
        public void SetAudioPitch(float pitch)
        {
            if (Sfx != null) Sfx.SetPitch(pitch);
        }
        private IEnumerator PoolRoutine(float duration)
        {
            if(!SpawnedFromPool) yield break;
            if(Sfx != null && Sfx.Enqueued)
            {
                yield break;
            }
            if (duration < 0)
            {
                duration = Vfx.GetDuration();
            }
            yield return new WaitForSeconds(duration);
            
            if(Vfx != null) Vfx.Stop();
            if(Sfx != null) Sfx.Stop();
            if(PlayedShaderEffect != null) PlayedShaderEffect.StopShaderEffect();
            Despawn();
        }

        private IEnumerator _despawnRoutine;
        public void Despawn()
        {
            if (SpawnedFromPool)
            {
                if (_despawnRoutine != null)
                {
                    StartCoroutine(_despawnRoutine);
                }
                _despawnRoutine = DespawnRoutine();
                StartCoroutine(_despawnRoutine);
            }
        }

        private IEnumerator DespawnRoutine()
        {
            yield return new WaitForSeconds(DespawnDelay);
            _despawnRoutine = null;
            EffectsLibrary.GetContext<EffectsLibrary>().EffectsPool.PoolObject(gameObject);
        }
        
        public void OnSoundDequeued()
        {
            Sfx.Enqueued = false;
            StartCoroutine(PoolRoutine(Duration));
        }
    }
}