using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{    
    /// <summary>
    /// This class represents a collection of runner chunk elements. The purpose is to see the width in the editor. Then a generated chunk can 
    /// read the depth from this component. This way, generated chunk doesn't have to be limited with prefabs having the same depth.
    /// </summary>
    public class RunnerChunkElements : MonoBehaviour {
        public int MinLevel = 0;
        public int MaxLevel = 100;
        public bool DrawDebugLines;
        public float Width = 15f;
        public float Depth = 10f;
        public float Height = 5f;
        public Vector3 Offset = Vector3.zero;

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!DrawDebugLines) return;
            Gizmos.DrawWireCube(transform.forward * Depth * 0.5f + transform.position,
            new Vector3(Width, Height, Depth));
        }
#endif
    }
}