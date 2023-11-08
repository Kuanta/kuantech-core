using UnityEngine;
namespace Kuantech.Core.HyperCasual
{
    public class DestructiblePiece : MonoBehaviour
    {
        public Rigidbody Rigidbody;
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;

        public void Initialize()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            Rigidbody = GetComponent<Rigidbody>();
        }

        public void Reset()
        {
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            transform.localPosition = _initialLocalPosition;
            transform.localRotation = _initialLocalRotation;
        }
      
    }
}