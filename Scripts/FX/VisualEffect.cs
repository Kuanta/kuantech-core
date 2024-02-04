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
    }
}