using UnityEngine;

namespace Kuantech.Core.FX
{
    public class VisualEffect : MonoBehaviour {
        [SerializeField] private ParticleSystem ParticleEffect;
        [SerializeField] private bool Emit;
        [SerializeField] private int EmitCount = 1;

        [SerializeField] private bool PlayWithEffectsManager;

        public void Play()
        {
            if(Emit)
            {
                ParticleEffect.Emit(EmitCount);
                return;
            }
            ParticleEffect.Play();
        }

      
        public void Stop()
        {
            ParticleEffect.Stop();
        }

        public float GetDuration()
        {
            return ParticleEffect.main.duration;
        }

        public virtual void SetColor(Color color)
        {
            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
            foreach(var particle in particleSystems)
            {
                var mainModule = particle.main;
                mainModule.startColor = color;
            }
           
        }
    }
}