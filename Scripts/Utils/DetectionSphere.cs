using System;
using UnityEngine;

namespace Kuantech
{
    [RequireComponent(typeof(Collider))]
    public class DetectionSphere : MonoBehaviour
    {
        public EventHandler<GameObject> TargetEntered;
        public EventHandler<GameObject> TargetExited;

        private void OnTriggerEnter(Collider other)
        {
            TargetEntered?.Invoke(this, other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            TargetExited?.Invoke(this, other.gameObject);
        }
    }
}