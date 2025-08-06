using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{        
    [ExecuteInEditMode]
    public class BeamFx : MonoBehaviour
    {
        [Header("Fx modules")]
        [SerializeField]
        private List<LineRenderer> LineRenderers;
        
        [SerializeField]
        private List<ParticleSystem> ParticleSystems;

        [SerializeField] private ParticleSystem MuzzleFx;
        [SerializeField] private ParticleSystem ImpactFx;
        
        [Header("Cast Points")] 
        [SerializeField] private Transform StartPoint;
        [SerializeField] private Transform EndPoint;
        
        private void Update()
        {
            if (!(LineRenderers.IsNullOrEmpty() || StartPoint == null || EndPoint == null))
            {
                foreach (var lineRenderer in LineRenderers)
                {
                    if (lineRenderer == null) continue;
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, StartPoint.position);
                    lineRenderer.SetPosition(1, EndPoint.position);
                }
            }

            if (MuzzleFx != null && MuzzleFx != null)
            {
                MuzzleFx.transform.position = StartPoint.position;
                MuzzleFx.transform.forward = StartPoint.forward;
                MuzzleFx.Play();
            }

            if (ImpactFx != null && EndPoint != null)
            {
                ImpactFx.transform.position = EndPoint.position;
                ImpactFx.transform.forward = EndPoint.forward;
                ImpactFx.Play();
            }
        }
    }
}