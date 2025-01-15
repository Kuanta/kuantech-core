using DG.Tweening;
using Kuantech.Core.Utils;
using UnityEngine;

namespace Kuantech.Core.Camera
{
    public class KtCamera : MonoBehaviour
    {
        public UnityEngine.Camera Camera;
        public GameObject Rig;
        public CameraEffects CameraEffects;

        [Header("Camera Shake")] 
        public float ShakeDuration = 0.5f;
        public float ShakeStrength = 1.0f;
        public int Vibrato = 10;
        private float Randomness = 90.0f;
        
        public void ShakeCamera()
        {
            if (Camera != null)
            {
                Camera.transform.DOShakePosition(ShakeDuration, ShakeStrength, Vibrato, Randomness)
                    .OnComplete(() => Camera.transform.localPosition = Vector3.zero);
            }
        }
    }
}