using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public class ShaderEffect : MonoBehaviour
    {
        [SerializeField] private List<SpriteRenderer> SpriteRenderers;
        [NonSerialized] public List<Material> MaterialInstances;
        
        private void Awake()
        {
            MaterialInstances = new List<Material>();
            if (!SpriteRenderers.IsNullOrEmpty())
            {
                foreach (var sr in SpriteRenderers)
                {
                    if (sr == null) continue;
                    Material instancedMat = sr.material;
                    MaterialInstances.Add(instancedMat);
                }
            }
        }

        public virtual void PlayShaderEffect()
        {
            
        }

        public virtual void Reset()
        {
            StopAllCoroutines();
        }
    }
}