using UnityEngine;

namespace Kuantech.Core.Utils
{
    [ExecuteInEditMode]
    public class ObjectRotator : MonoBehaviour
    {
        [SerializeField] private float RotationSpeed = 10f;
        [SerializeField] private Vector3 RotationAxis = Vector3.forward;
        [SerializeField] private Vector3 MovementAxis;
        [SerializeField] private float MovementMagnitude = 0;
        [SerializeField] private float MovementFrequency = 1f;
        private void Update()
        {
            if (transform.parent != null)
            {
                transform.localPosition = MovementAxis * Mathf.Sin(Time.time * MovementFrequency) * MovementMagnitude;
            }
            transform.localRotation *= Quaternion.AngleAxis(RotationSpeed * Time.deltaTime, RotationAxis);
        }

        public void SetRotationSpeed(float speed)
        {
            RotationSpeed = speed;
        }
    }
}