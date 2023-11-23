using System.Collections;
using Kuantech.Core.Utils;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class Trap : MonoBehaviour, IChunkElement
    {
        
        [Header("Collision Events")] 
        [SerializeField] protected CollisionEventsRelayer TrapTrigger;
        [SerializeField] protected float ApplyEffectFrequency = 0f;
        protected float ApplyCounter = 0f;
        private RpgActor _enteredActor = null;


        [SerializeField] protected AudioSource EnableSfx;
        [SerializeField] protected AudioSource DisableSfx;
        
        //Perodic enabling
        [SerializeField] protected bool PeriodicEnable = true; // If true, trap will periodically enable disable
        [SerializeField] protected float EnabledTime = 1f;
        [SerializeField] protected float DisabledTime = 1f;
        [SerializeField] protected float ToggleDelay = 0.5f;
        public float EnableTimerOffset = 0f;
        protected bool Enabled;
        protected float EnableTimer = 0f;
        protected bool IsTransitioning = false; //Trap is being enabled or disabled

        private void Start()
        {
            if (TrapTrigger == null) return; //Not every trap needs trigger area (see arrow trap)
            TrapTrigger.OnTriggerEnterEvent += OnActorEnter;
            TrapTrigger.OnTriggerExitEvent += OnActorExit;
        }

        #region Level Lifecycle
        public void OnChunkGenerated(RunnerChunk chunk)
        {
            Reset();
        }

        public void OnChunkRestart()
        {
            Reset();
        }

        public void OnPlayerEnteredChunk()
        {
            if (PeriodicEnable) return;
            EnableTrap();
        }

        public void OnPlayerExitedChunk()
        {
            if (PeriodicEnable) return;
            DisableTrap();
        }

        public void OnClearChunk()
        {
            Reset();
        }
        #endregion
    
        
        protected virtual void Update()
        {
            if (IsTransitioning) return;

            switch (PeriodicEnable)
            {
                //If periodic
                case true when !Enabled && EnableTimer >= DisabledTime:
                    EnableTrap();
                    EnableTimer = 0f;
                    return;
                case true when Enabled && EnableTimer >= EnabledTime:
                    DisableTrap();
                    EnableTimer = 0f;
                    return;
                case true:
                    EnableTimer += Time.deltaTime;
                    break;
            }

            if (!Enabled) return;

            if (_enteredActor == null || ApplyEffectFrequency == 0f) return;
            ApplyCounter += Time.deltaTime;
            if (ApplyCounter >= ApplyEffectFrequency)
            {
                ApplyTrapEffect(_enteredActor);
                ApplyCounter -= ApplyEffectFrequency;
            }
        }
        
        //Enables trap
        protected virtual void EnableTrap()
        {
            StartCoroutine(ToggleRoutine(true));
        }
        
        //Disables trap
        protected virtual void DisableTrap()
        {
            StartCoroutine(ToggleRoutine(false));  
        }
        
        
        /// <summary>
        /// Applies any effect that the trap is supposed to do
        /// </summary>
        protected virtual void ApplyTrapEffect(RpgActor actor)
        {
            //Defautl behaviour is to damage target
        }
        protected IEnumerator ToggleRoutine(bool toggle)
        {
            yield return new WaitForSeconds(ToggleDelay);
            if (toggle && EnableSfx != null)
            {
                EnableSfx.Play();
            }else if (!toggle && DisableSfx != null)
            {
                DisableSfx.Play();
            }
            
            Enabled = toggle;
            if (TrapTrigger != null)
            {
                _enteredActor = null; //if set to null, exit doesn't trigger
                TrapTrigger.gameObject.SetActive(toggle);
            }
        }

        
        protected virtual void OnActorEnter(object sender, Collider other)
        {
            RpgActor actor = other.gameObject.GetComponent<RpgActor>();
            if (actor == null) return;
            _enteredActor = actor;
            
            ApplyTrapEffect(actor);
        }

        protected virtual void OnActorExit(object sender, Collider other)
        {
            RpgActor actor = other.gameObject.GetComponent<RpgActor>();
            if (actor == _enteredActor)
            {
                _enteredActor = null;
            }
        }

        
        protected virtual void Reset()
        {
            gameObject.SetActive(true);
            ApplyCounter = 0f;
            _enteredActor = null;

            Enabled = !PeriodicEnable;
            
            //Traps start as disabled. In order to offset at the beginning we add offset*disabled time
            EnableTimer = EnableTimerOffset * DisabledTime;
            IsTransitioning = false;
            StopAllCoroutines();
        }

    }
}