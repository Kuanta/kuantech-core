using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class RunnerChunkDebugLines : MonoBehaviour {

        [Header("Debug Lines")]
        public bool DrawDebugLines;
        public float Width = 15f;
        public float Depth = 10f;
        public float Height = 5f;
        public Vector3 Offset = Vector3.zero;

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!DrawDebugLines) return;
            Gizmos.DrawWireCube(Offset + transform.position,
            new Vector3(Width, Height, Depth));
        }
#endif
    }

}