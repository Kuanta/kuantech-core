using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class VisualDetector : MonoBehaviour
    {
        public float VisualWidth = 20f;
        public float VisualDepth = 10f;
        public float VisualHeight = 5f;
        public float DepthOffset = 0.5f;
        public List<Collider> DetectedColliders = new List<Collider>();
        public LayerMask LayerMask;
        public float UpdateFrequency = 1f;
        private Collider[] _results = new Collider[32];

        public delegate bool VisualDetectorChecker(Collider collider);

        public VisualDetectorChecker CheckerHandler;
        private float _lastUpdateTime = 0;
        
        private void Update()
        {
            if (!(Time.time - _lastUpdateTime > UpdateFrequency)) return;
            Detect(CheckerHandler);
            _lastUpdateTime = Time.time;
        }

        public void Detect(VisualDetectorChecker handler)
        {
            DetectedColliders.Clear();
            Vector3 center = transform.position + transform.forward * (VisualDepth * 0.5f + DepthOffset);
            int hitCount = UnityEngine.Physics.OverlapBoxNonAlloc(center,
                new Vector3(VisualWidth * 0.5f, VisualHeight * 0.5f, VisualDepth * 0.5f),
                _results, transform.rotation, LayerMask);
            for (int i = 0; i < hitCount; ++i)
            {
                if(handler == null) DetectedColliders.Add(_results[i]);
                else if(handler(_results[i])) DetectedColliders.Add(_results[i]);
            }
        }
    }
}