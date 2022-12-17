using System;
using UnityEngine;

namespace Kuantech.EndlessRunner
{
    public class Pickupable : MonoBehaviour, ISpawnable
    {
        public float AngulerSpeed = 0f;
        public float FloatSpeed = 0f;
        public float FloatDistance = 0f;

        protected float FloatNormalizedPosition = 0f;
        protected int FloatDirection = 1;
        protected Vector3 InitialPosition;

        [Header("Components")] 
        [SerializeField] private GameObject Model;
        [SerializeField] private Collider Collider;
        private bool _available = true;
        
        [Header("Effects")] [SerializeField] private AudioSource PickupSound;
        
        protected virtual void Update()
        {
            if (!_available) return;
            transform.RotateAround(transform.position, Vector3.up, Time.deltaTime*AngulerSpeed); 
        }
        
        protected virtual void OnPickup()
        {
            Model.SetActive(false);
            Collider.enabled = false;
            _available = false;
            if(PickupSound != null) PickupSound.Play();
        }
        
        public void OnTriggerEnter(Collider collider)
        {
            OnPickup();
        }

        public virtual void OnSpawn(Vector3 position, Quaternion rotation)
        {
            Reset();
            InitialPosition = position;
            FloatNormalizedPosition = 0f;
            FloatDirection = 1;
            if(PickupSound != null) PickupSound.Stop();
        }

        public void OnRespawn(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
            Reset();
            if(PickupSound != null) PickupSound.Stop();
        }

        public void Reset()
        {
            Model.SetActive(true);
            Collider.enabled = true;
            transform.rotation = Quaternion.identity;
            _available = true;
        }
    }
}