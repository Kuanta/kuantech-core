using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public class VisualEffect : MonoBehaviour {
        [SerializeField] private ParticleSystem ParticleEffect;
        [SerializeField] private bool Emit;
        [SerializeField] private int EmitCount = 1;
        [SerializeField] private List<ParticleSystem> ChildEmitters;
        [SerializeField] private bool PlayWithEffectsManager;

        public void Play(EffectPlaySettings settings)
        {
            //todo: This can be done better
            if(Emit)
            {
                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
                emitParams.position = settings.PlayPosition;
                if (ChildEmitters != null)
                {
                    foreach (var childEmitter in ChildEmitters)
                    {
                        childEmitter.Emit(emitParams, EmitCount);
                    } 
                }
                ParticleEffect.Emit(emitParams, EmitCount);
                return;
            }
            if(ParticleEffect !=null) ParticleEffect.Play();
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