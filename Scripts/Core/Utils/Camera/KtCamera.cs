using DG.Tweening;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.Camera
{
    public class KtCamera : MonoBehaviour
    {
        public UnityEngine.Camera Camera;
        public GameObject Rig;

        [Header("Camera Shake")] 
        public float ShakeDuration = 0.5f;
        public float ShakeStrength = 1.0f;
        public int Vibrato = 10;
        private float Randomness = 90.0f;

        #region Camera Effects
        /// <summary>
        /// Shakes with default values
        /// </summary>
        public void ShakeCamera()
        {
            ShakeCamera(ShakeStrength, ShakeDuration, Vibrato);
        }
        
        /// <summary>
        /// Shakes the camera with given parameters
        /// </summary>
        /// <param name="shakesStrength"></param>
        /// <param name="shakeDuration"></param>
        /// <param name="vibrato"></param>
        public void ShakeCamera(float shakesStrength, float shakeDuration, int vibrato)
        {
            if (Camera != null)
            {
                Camera.transform.DOKill();
                Camera.transform.DOShakePosition(shakeDuration, shakesStrength, vibrato, Randomness)
                    .OnComplete(() => Camera.transform.localPosition = Vector3.zero);
            }
        }
        #endregion


        public void SetCameraPosition(WorldPoint point)
        {
            transform.position = point.GetTargetPosition();
            transform.rotation = point.GetRotation();
        }

    }
}