using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class CameraDistanceCuller : MonoBehaviour
    {
        [SerializeField] private Camera Camera;
        [SerializeField] private float[] CullDistances = new float[32];
        public bool enabled = true;
        private void Awake()
        {
            if (!enabled) return;
            Camera.layerCullDistances = CullDistances;
        }
    }
}