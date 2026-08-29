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
        [Tooltip("The effect's own base playback speed. Combined with EffectPlaySettings.EffectSpeedMultiplier (e.g. attack speed).")]
        public float SpeedMultiplier = 1f;
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
        [SerializeField] protected List<FxBehaviour> EffectBehaviours = new List<FxBehaviour>();

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
            if ((Time.time - _lastPlayedTime > GetDuration() && GetDuration() > 0))
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
            foreach (var behaviour in EffectBehaviours)
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

            float duration = GetDuration();
            if (settings.DespawnAfterPlay && duration > 0)
            {
                _stopRoutine = PoolRoutine(duration);
                StartCoroutine(_stopRoutine);
            }
            else if (duration > 0)
            {
                _stopRoutine = StopRoutine();
                StartCoroutine(_stopRoutine);
            }
        }
        
        #region Timings

        /// <summary>
        /// Combined playback speed: the effect's own <see cref="SpeedMultiplier"/> times the play settings'
        /// EffectSpeedMultiplier (e.g. attack speed). Each factor is guarded to a sane minimum so a 0/unset
        /// value never freezes the effect or divides by zero.
        /// </summary>
        public float GetSpeedMultiplier()
        {
            float own = SpeedMultiplier > 0f ? SpeedMultiplier : 1f;
            float play = EffectPlaySettings.EffectSpeedMultiplier > 0f ? EffectPlaySettings.EffectSpeedMultiplier : 1f;
            return own * play;
        }

        // Faster playback → shorter delay/duration. Sentinels (<= 0, e.g. looping) are left untouched.
        public float GetDelay()
        {
            return Delay > 0f ? Delay / GetSpeedMultiplier() : Delay;
        }

        public float GetDuration()
        {
            return Duration > 0f ? Duration / GetSpeedMultiplier() : Duration;
        }

        #endregion
        
        #region Utility Play Overloads

        public void Play(Transform parent, Vector3 localPosition, Quaternion localRotation, float effectCooldown = -1)
        {
            EffectPlaySettings settings = EffectPlaySettings.GetPlayAtObjectSettings(parent, localPosition, localRotation);
            settings.EffectCooldown = effectCooldown;
            Play(settings);
        }

        public void Play(Vector3 position, Quaternion rotation, float effectCooldown = -1)
        {
            EffectPlaySettings settings = EffectPlaySettings.GetPlayAtPositionSettings(position, rotation);
            settings.EffectCooldown = effectCooldown;
            Play(settings);
        }

        public void PlayTimed(float duration, Transform parent, Vector3 localPosition, Quaternion localRotation, float effectCooldown = -1)
        {
            float prevDuration = Duration;
            Duration = duration;
            Play(parent, localPosition, localRotation, effectCooldown);
            Duration = prevDuration;
        }

        public void PlayTimed(float duration, Vector3 position, Quaternion rotation, float effectCooldown = -1)
        {
            float prevDuration = Duration;
            Duration = duration;
            Play(position, rotation, effectCooldown);
            Duration = prevDuration;
        }

        #endregion

        public void _Play(EffectPlaySettings playSettings)
        {
            StartCoroutine(PlayRoutine(playSettings));
        }

        private IEnumerator PlayRoutine(EffectPlaySettings playSettings)
        {
            yield return new WaitForSeconds(GetDelay());
            PlayEffects(playSettings);
        }

        private IEnumerator StopRoutine()
        {
            yield return new WaitForSeconds(GetDuration());
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

            float speed = GetSpeedMultiplier();

            //Sound
            if (!EffectsLibrary.CanPlayEffect(EffectId, playSettings.EffectCooldown)) return;
            if(!EffectsLibrary.PlayAudio(AudioTag))
            {
                if (Sfx != null)
                {
                    Sfx.ComboFromEffect = playSettings.ComboIndex;
                    Sfx.SetSpeedMultiplier(speed); // pitch (and fire rate) scale with playback speed
                    Sfx.PlayThroughAudioLibrary();
                }
            }

            //Visual Effect
            if (Vfx != null) Vfx.Play(playSettings, speed);

            //Animation
            if (Animator != null)
            {
                Animator.speed = speed;
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

            if (!EffectBehaviours.IsNullOrEmpty())
            {
                foreach (var behaviour in EffectBehaviours)
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
            if (!EffectBehaviours.IsNullOrEmpty())
            {
                foreach (var behaviour in EffectBehaviours)
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
            StartCoroutine(PoolRoutine(GetDuration()));
        }
    }
}
