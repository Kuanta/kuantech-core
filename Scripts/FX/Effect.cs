using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Utils;
using Sirenix.OdinInspector;
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
        public AnimationData AnimationData;
        
        [Header("Shader Effect")] 
        [Tooltip("If set to true, renderers will be detected")] public bool DetectAllRenderers = true;
        public ShaderEffect ShaderEffect;
        public string ShaderEffectId;
        [NonSerialized] public ShaderEffect PlayedShaderEffect;
        
        [Header("Effect Behaviours")]
        [SerializeField] private List<FxBehaviour> _effectBehaviours = new List<FxBehaviour>();

        //If an effect is under the protection of effects library, it can't be destroyed with timed calls
        [NonSerialized] public bool SpawnedFromPool = false; //This is used to determine if the effect was spawned from the pool or not. 
        [NonSerialized] public EffectsModule OwnerEffectModule; //Effects may be owned by actors
        [NonSerialized] public EffectPlaySettings EffectPlaySettings; //This is used to store the settings used to play the effect

        private IEnumerator _stopRoutine = null;
        private IEnumerator _despawnRoutine = null;

        // App kapanırken OnDisable temizliği tetiklenmesin diye
        private bool _isQuitting = false;
        private void OnApplicationQuit() => _isQuitting = true;

        [NonSerialized] public bool IsFxPlaying;
        private float _lastPlayedTime;

        /// <summary>
        /// Parent yüzünden pasif olma durumunu yakala: 
        /// gameObject.activeSelf == true && activeInHierarchy == false
        /// Bu durumda coroutine’ler çalışmayacağı için cleanup’ı anında yap.
        /// </summary>
        private void OnDisable()
        {
            if (_isQuitting) return;

            // Her durumda bu objeye bağlı tüm coroutineleri iptal et
            if (_stopRoutine != null) { StopCoroutine(_stopRoutine); _stopRoutine = null; }
            if (_despawnRoutine != null) { StopCoroutine(_despawnRoutine); _despawnRoutine = null; }

            bool deactivatedByHierarchy = gameObject.activeSelf && !gameObject.activeInHierarchy;

            if (deactivatedByHierarchy)
            {
                Stop(); 
                if (SpawnedFromPool)
                {
                    _Despawn();
                }
            }
        }

        public bool IsPlaying()
        {
            if ((Time.time - _lastPlayedTime > Duration && Duration > 0))
            {
                IsFxPlaying = false;
            }
            return IsFxPlaying;
        }

        /// <summary>
        /// To simply play
        /// </summary>
        [Button("Play")]
        public void Play()
        {
            Play(EffectPlaySettings.GetDefaultSettings());
        }

        public void Update()
        {
            if (!IsPlaying()) return;
            foreach (var behaviour in _effectBehaviours)
            {
                behaviour.UpdateFx();
            }
        }
        /// <summary>
        /// Plays the effect using the settings
        /// </summary>
        /// <param name="settings"></param>
        public void Play(EffectPlaySettings settings)
        {
         
            EffectPlaySettings = settings;
            if (settings.EffectParent != null)
            {
                transform.SetParent(settings.EffectParent);
                transform.localPosition = settings.LocalPlayPosition;
                transform.localRotation = settings.LocalPlayRotation;
            }
            else
            {
                if (settings.SetPosition)
                {
                    transform.position = settings.PlayStartPosition;
                }

                if (settings.SetRotation)
                {
                    transform.rotation = settings.PlayStartRotation;
                }
            }

            _Play(settings);

            if (_stopRoutine != null)
            {
                StopCoroutine(_stopRoutine);
            }

            // Zamanlamaya göre otomatik durdurma/pool
            if (settings.DespawnAfterPlay && Duration > 0)
            {
                _stopRoutine = PoolRoutine(Duration);
                StartCoroutine(_stopRoutine);
            }
            else if (Duration > 0)
            {
                _stopRoutine = StopRoutine();
                StartCoroutine(_stopRoutine);
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

        private IEnumerator StopRoutine()
        {
            yield return new WaitForSeconds(Duration);
            Stop();
        }

        protected virtual void PlayEffects(EffectPlaySettings playSettings)
        {
            IsFxPlaying = true;
            _lastPlayedTime = Time.time;
            
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
            if (Animator != null)
            {
                AnimationData.SetParameters(Animator);
            }
            
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

            if (!_effectBehaviours.IsNullOrEmpty())
            {
                foreach (var behaviour in _effectBehaviours)
                {
                    behaviour.StartFxBehaviour(this);
                }
            }
            
            EffectsLibrary.SetLastPlayedTime(EffectId);
        }

        public void Stop()
        {
            IsFxPlaying = false;
            
            // VFX
            if(Vfx!=null) Vfx.Stop();
            
            // SFX
            if (Sfx != null) Sfx.Stop(SfxFadeOutDuration);
            
            // Shader
            if (PlayedShaderEffect != null)
            {
                PlayedShaderEffect.StopShaderEffect();
            }
            
            // Behaviours
            if (!_effectBehaviours.IsNullOrEmpty())
            {
                foreach (var behaviour in _effectBehaviours)
                {
                    behaviour.OnFxEnded();
                }
            }
            
            // Havuzdan geldiyse, normal akışta Despawn iste
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
            _stopRoutine = null;
            Stop();
        }

        public void Despawn(bool immediate=false)
        {
            // Bu objeye bağlı tüm coroutineleri iptal et
            if (_stopRoutine != null) { StopCoroutine(_stopRoutine); _stopRoutine = null; }
            if (_despawnRoutine != null) { StopCoroutine(_despawnRoutine); _despawnRoutine = null; }

            if (SpawnedFromPool)
            {
                // Hiyerarşi pasifken coroutine çalışamayacağından HER DURUMDA anında despawn et
                if (immediate || !gameObject.activeInHierarchy)
                {
                    _Despawn();
                    return;
                }

                // Aksi halde gecikmeli despawn
                _despawnRoutine = DespawnRoutine();
                StartCoroutine(_despawnRoutine);
            }
        }

        private IEnumerator DespawnRoutine()
        {
            yield return new WaitForSeconds(DespawnDelay);
            _Despawn();
        }

        public void Cleanup()
        {
            Despawn(true);
        }
        
        private void _Despawn()
        {
            _stopRoutine = null;
            _despawnRoutine = null;
            //Pool effects deferred cause they may be pooled during OnDisable
            EffectsLibrary.GetContext<EffectsLibrary>().EffectsPool.PoolObjectDeferred(gameObject);
        }

        public void OnSoundDequeued()
        {
            Sfx.Enqueued = false;
            StartCoroutine(PoolRoutine(Duration));
        }
    }
}
