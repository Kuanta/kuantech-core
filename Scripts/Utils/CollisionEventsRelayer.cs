using System;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    //A class that simply relays OnTrigger events
    public class CollisionEventsRelayer : MonoBehaviour
    {
        public EventHandler<Collider> OnTriggerEnterEvent;
        public EventHandler<Collider> OnTriggerExitEvent;
        public EventHandler<Collision> OnCollisionEnterEvent;
        public EventHandler<Collision> OnCollisionExitEvent;

        private void OnTriggerEnter(Collider other)
        {
            OnTriggerEnterEvent?.Invoke(this, other);
        }

        private void OnTriggerExit(Collider other)
        {
            OnTriggerExitEvent?.Invoke(this,other);
        }

        private void OnCollisionEnter(Collision other)
        {
            OnCollisionEnterEvent?.Invoke(this, other);
        }

        private void OnCollisionExit(Collision other)
        {
            OnCollisionExitEvent?.Invoke(this, other);
        }
    }
}