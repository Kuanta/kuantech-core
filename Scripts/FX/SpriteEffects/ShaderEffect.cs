using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public class ShaderEffect : MonoBehaviour
    {
        public string EffectId;
        [NonSerialized] public List<Material> MaterialInstances;

        public static List<Material> DetectMaterialsUnderGameobject(GameObject root)
        {
            List<Material> materialInstances = new List<Material>();
            
            if (root == null)
            {
                Debug.LogWarning("DetectAllRenderers: Root is null.");
                return null;
            }

            // 1. SpriteRenderer (2D)
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null) continue;
                materialInstances.Add(sr.material);
            }

            // 2. MeshRenderer (3D static)
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null) continue;
                foreach (var mat in mr.materials)
                {
                    if (mat != null)
                        materialInstances.Add(mat);
                }
            }

            // 3. SkinnedMeshRenderer (3D rigged)
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr == null) continue;
                foreach (var mat in smr.materials)
                {
                    if (mat != null)
                        materialInstances.Add(mat);
                }
            }

            return materialInstances;
        }
        public void DetectAllRenderers(GameObject root)
        {
            MaterialInstances = DetectMaterialsUnderGameobject(root);
        }

        public virtual void PlayShaderEffect()
        {
            
        }

        public virtual void StopShaderEffect()
        {
            
        }
        
        public virtual void Reset()
        {
            StopAllCoroutines();
        }

        #region Parameter setters

        protected void SetColorProperty(string propertyName, Color color)
        {
            foreach (var mat in MaterialInstances)
            {
                if (mat.HasProperty(propertyName))
                {
                    mat.SetColor(propertyName, color);
                }
            }
        }

        #endregion
    }
}