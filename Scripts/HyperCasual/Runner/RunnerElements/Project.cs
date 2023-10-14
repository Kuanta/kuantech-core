using System;
using Kuantech.Core.FX;
using Kuantech.Core.HyperCasual;
using Kuantech.Core.HyperCasual.Runner;
using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.JobTrainer
{
    public class Project : Pickupable {
        [Header("Properties")]
        [SerializeField] private float ProjectDuration = 3f;
        [SerializeField] private float TargetProgress = 100f;
        [SerializeField] private float ProgressMultiplier = 1f;
        [SerializeField] private float TapCooldown = 0.1f;
        
        [Header("Visuals")]
        [SerializeField] private GameObject ProgressParent;
        [SerializeField] private Effect WorkingEffect;
        [SerializeField] private Fillbar TimeFillbar;
        [SerializeField] private Fillbar ProgressFillbar;
        [SerializeField] private Effect SuccessEffect;
        [SerializeField] private Effect FailureEffect;

        private bool _projectStarted;
        private float _projectStartTime;
        private float _averageTraitValue;
        private JobTypes _jobType;
        private float _currentProgress = 0f;
        private float _lastTapTime;
        

        public override void Spawn()
        {
            base.Spawn();
            VirtualJoystick vj = FindObjectOfType<VirtualJoystick>();
            vj.TapEvent += OnTap;
        }

        private void OnTap(object sender, EventArgs e)
        {
            if(!_projectStarted || Time.time - _lastTapTime < TapCooldown) return;
            _lastTapTime = Time.time;
            _currentProgress += ProgressMultiplier * _averageTraitValue;
        }


        protected override void OnPickup(Collider other)
        {
            base.OnPickup(other);
            StartProgress();
        }

        private void StartProgress()
        {
            WorkerCrowd crowd =
                  RunnerManager.GetContext<RunnerManager>().Runner as WorkerCrowd;
            TraitTypes maxTrait = crowd.GetHighestTrait();
            _averageTraitValue = crowd.GetAverageTraitValue(maxTrait);

            _projectStartTime = Time.time;
            _projectStarted = true;
            _currentProgress = 0f;
            crowd.InputLock.Lock();
            crowd.FrontMovementBlocked = true;
            ProgressParent.SetActive(true);
            WorkingEffect.Play();
        }

        protected override void Update()
        {
            base.Update();
            if(!_projectStarted || LevelManager.GetCurrentState() != LevelState.Playing) return;

            float normalizedTime = (Time.time - _projectStartTime) / ProjectDuration;
            float normalizedProgress = _currentProgress / TargetProgress;

            if(normalizedProgress >= 1f)
            {
                Complete(true);
                return;
            }

            if(normalizedTime >= 1f)
            {
                Complete(false);
                return;
            }

            if(TimeFillbar != null) TimeFillbar.SetFill(1 - normalizedTime);
            if(ProgressFillbar != null) ProgressFillbar.SetFill(normalizedProgress);

        }

        private void Complete(bool success)
        {
            ProgressParent.SetActive(false);

            if (success) OnSucces();
            else OnFailure();
            _projectStarted = false;
            Invoke("ReleaseRunner", 2f);
        }

        /// <summary>
        /// Releases the runner
        /// </summary>
        private void ReleaseRunner()
        {
            WorkerCrowd crowd = RunnerManager.GetContext<RunnerManager>().Runner as WorkerCrowd;
            crowd.InputLock.Unlock();
            crowd.FrontMovementBlocked = false;
            
        }
        private void OnSucces()
        {
            Debug.LogError("SUCCESS");
            if(SuccessEffect != null) SuccessEffect.Play();
        }

        private void OnFailure()
        {
            Debug.LogError("FAILURE");
            if(FailureEffect != null) FailureEffect.Play();
        }

      
        public override void Reset()
        {
            base.Reset();
            WorkingEffect.Stop();
            ProgressParent.SetActive(false);
            _projectStarted = false;
            _currentProgress = 0f;
        }

    }
}