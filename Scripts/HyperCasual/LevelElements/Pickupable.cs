using Kuantech.Core.FX;
using Kuantech.EndlessRunner;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class Pickupable : LevelElement
    {
        public float AngulerSpeed = 0f;
        public float FloatSpeed = 0f;
        public float FloatDistance = 0f;

        protected float FloatNormalizedPosition = 0f;
        protected int FloatDirection = 1;

        [Header("Components")] 
        [SerializeField] private GameObject Model;
        [SerializeField] protected Collider Collider;
        protected bool Available = true;

        [Header("Effects")] 
        [SerializeField] private AudioSource PickupSound;
        [SerializeField] protected AudioTypes PickupUISound = AudioTypes.None;
        
        protected virtual void Update()
        {
            if (!Available || Model == null) return;
            Model.transform.RotateAround(transform.position, Vector3.up, Time.deltaTime*AngulerSpeed); 
        }
        
        protected virtual void OnPickup()
        {
            Disable();
            if (PickupSound != null)
            {
                PickupSound.Play();
            }else if (PickupUISound != AudioTypes.None)
            {
                EffectsLibrary.Instance.AudioLibrary.PlaySound(PickupUISound);
            }
        }
        
        public virtual void OnTriggerEnter(Collider collider)
        {
            OnPickup();
        }

        public virtual void Spawn()
        {
            Reset();
            Enable();
            FloatNormalizedPosition = 0f;
            FloatDirection = 1;
            if(PickupSound != null) PickupSound.Stop();
        }

        public virtual void OnPlayerEnteredChunk()
        {
            
        }

        public void Toggle(bool toggle)
        {
            if(toggle) Reset();
            else Disable();
        }

        public virtual void Enable()
        {
            if(Model != null) Model.SetActive(true);
            if (Collider == null)
            {
                Debug.LogError("WTYF");
            }
            Collider.enabled = true;
            Available = true;
        }
        public virtual void Disable()
        {
            if(Model != null) Model.SetActive(false);
            Collider.enabled = false;
            Available = false;
        }
        public virtual void Reset()
        {
            transform.rotation = Quaternion.identity;
            if (Model == null) return;
            Model.transform.localRotation = Quaternion.identity;
        }

        public override void OnPrepareLevel()
        {
            Spawn();
        }

        public override void OnLeaveLevel()
        {
        }

        public override void OnPlayLevel()
        {
        }

        public override void OnPlayerEntered()
        {
        }

        public override void OnPlayerExited()
        {
        }
    }
}