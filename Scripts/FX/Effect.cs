using System.Collections;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public class Effect : MonoBehaviour
    {
        public EffectTypes EffectType;
        public AudioSource Sfx;
        public ParticleSystem Vfx;
        public float Duration;
        public float Delay = 0f;
        public Animator Animator;
        private static readonly int Play1 = Animator.StringToHash("Play");
        public bool EmitEffect = false; //If set to true, effect will be emitted instead of play
        public int EmitCount = 1;

        public void Play()
        {
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            yield return new WaitForSeconds(Delay);
            PlayEffects();
        }

        private void PlayEffects()
        {
            if(Sfx != null) Sfx.Play();
            if(Vfx != null && !EmitEffect) Vfx.Play();
            else if (Vfx != null && EmitEffect)
            {
                Vfx.transform.position = transform.position;
                Vfx.transform.forward = transform.forward;
                Vfx.Emit(EmitCount);
            }
            if(Animator != null) Animator.SetTrigger(Play1);
        }
        
        public void Play(Vector3 position, Quaternion rotation, bool local = false)
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
            Play();
        }

        public void Play(Transform parent)
        {
            Play(parent, Vector3.zero, Quaternion.identity);
        }

        public void Play(Transform parent, Vector3 position, Quaternion rotation)
        {
            transform.SetParent(parent);
            Play(position, rotation, local:true);
        }

        public void PlayTimed()
        {
            Play();
            StartCoroutine(PoolRoutine(Duration));
            
        }
        
        public void PlayTimed(float duration, Vector3 position, Quaternion rotation, bool local = false)
        {
            Play(position, rotation, local);
            StartCoroutine(PoolRoutine(duration));
        }
                
        public void PlayTimed(float duration, Transform parent)
        {
            Play(parent);
            StartCoroutine(PoolRoutine(duration));
        }
        
        public void PlayTimed(float duration, Transform parent, Vector3 position, Quaternion rotation)
        {
            Play(parent, position, rotation);
            StartCoroutine(PoolRoutine(duration));
        }
        
        public void Stop()
        {
            
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
            EffectsLibrary.Instance.EffectsPool.PoolObject(gameObject);
        }
    }
}