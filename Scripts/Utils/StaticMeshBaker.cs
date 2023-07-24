using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class StaticMeshBaker : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Animator Animator;
        [SerializeField] private SkinnedMeshRenderer SkinnedMeshRenderer;
        [SerializeField] private MeshRenderer MeshRenderer;
        [SerializeField] private MeshFilter MeshFilter;
        private Mesh _meshToBake;
        
        [Header("Updates")]
        public float UpdateFrequency;
        public bool UpdateMesh;

        [Header("Distance Based")] 
        public bool UseDistanceBasedBaking;
        public AnimationCurve DistanceToFrequencyCurve;
        public float MaxDistance = 10f;
        public float MinDistance = 5f;
        public float MinFreq = 0.01f;
        public float MaxFreq = 10f;
        
        private float _lastUpdateTime = 0;
        private bool _bakedFirstTime = false;
        private void OnEnable()
        {
            Animator.Update(0.25f);
            BakeToMesh();
        }

        private void BakeToMesh()
        {
            if (MeshRenderer == null)
            {
                GameObject staticMeshObject = new GameObject("Static Mesh");
                staticMeshObject.transform.SetParent(transform);
                MeshRenderer = staticMeshObject.AddComponent<MeshRenderer>();
                MeshFilter = staticMeshObject.AddComponent<MeshFilter>();
                MeshRenderer.materials = SkinnedMeshRenderer.materials;
            }
            if (_meshToBake == null)
            {
                _meshToBake = new Mesh();
            }
            else
            {
                _meshToBake.Clear();
            }

            MeshFilter.mesh = _meshToBake;
            SkinnedMeshRenderer.BakeMesh(_meshToBake);
            SkinnedMeshRenderer.enabled = false;
            MeshRenderer.enabled = true;
            Animator.enabled = false;
        }

        private void Update()
        {
            if (!UpdateMesh) return;
            UpdateFrequency = GetUpdateFrequency();
            if (UpdateFrequency <= MinFreq)
            {
                EnableSkinnedMeshRenderer();
                return;
            }

            if (UpdateFrequency >= MaxFreq)
            {
                DisableSkinnedMeshRenderer();
                return;
            }
            float deltaTime = Time.time - _lastUpdateTime;
            if (Time.time - _lastUpdateTime > UpdateFrequency)
            {
                Animator.Update(deltaTime);
                BakeToMesh();
                _lastUpdateTime = Time.time;
            }
        }
        
        private void EnableSkinnedMeshRenderer()
        {
            MeshRenderer.enabled = false;
            SkinnedMeshRenderer.enabled = true;
            Animator.enabled = true;
        }

        private void DisableSkinnedMeshRenderer()
        {
            if(_meshToBake == null) BakeToMesh();
            MeshRenderer.enabled = true;
            SkinnedMeshRenderer.enabled = false;
        }

        private float GetUpdateFrequency()
        {
            if (!UseDistanceBasedBaking) return UpdateFrequency;
            float dist = (Camera.main.transform.position - transform.position).magnitude;
            if (dist <= MinDistance) dist = 0f;
            float normalizedDist = Mathf.Min(dist / MaxDistance, 1f);
            return  DistanceToFrequencyCurve.Evaluate(normalizedDist) * MaxFreq;
        }
    }
}