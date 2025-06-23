using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class ObjectRotator : MonoBehaviour
    {
        [SerializeField] private float RotationSpeed = 10f;
        [SerializeField] private Vector3 RotationAxis = Vector3.forward;

        private void Update()
        {
            transform.localRotation *= Quaternion.AngleAxis(RotationSpeed * Time.deltaTime, RotationAxis);
        }

        public void SetRotationSpeed(float speed)
        {
            RotationSpeed = speed;
        }
    }
}