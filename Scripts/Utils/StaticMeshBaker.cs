using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class StaticMeshBaker : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer SkinnedMeshRenderer;
        [SerializeField] private MeshRenderer MeshRenderer;
        [SerializeField] private MeshFilter MeshFilter;
        private Mesh MeshToBake;

        public Animator Animator;
        public float UpdateFrequency;
        public bool UpdateMesh;

        private float _lastUpdateTime = 0;
        
        public void BakeToMesh()
        {
            if (MeshToBake == null)
            {
                MeshToBake = new Mesh();
            }
            else
            {
                MeshToBake.Clear();
            }

            MeshFilter.mesh = MeshToBake;
            SkinnedMeshRenderer.BakeMesh(MeshToBake);
            SkinnedMeshRenderer.enabled = false;
            MeshRenderer.enabled = true;
            Animator.enabled = false;
        }

        private void Update()
        {
            if (!UpdateMesh) return;
            float deltaTime = Time.time - _lastUpdateTime;
            if (Time.time - _lastUpdateTime > UpdateFrequency)
            {
                Animator.Update(deltaTime);
                BakeToMesh();
                _lastUpdateTime = Time.time;
            }
        }
        
        public void EnableSkinnedMeshRenderer()
        {
            MeshRenderer.enabled = false;
            SkinnedMeshRenderer.enabled = true;
        }
    }
}