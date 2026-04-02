using System;
using Kuantech.Core.Camera;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.Controller
{
    /// <summary>
    /// Player controller, handles the 
    /// </summary>
    [Serializable]
    public class PlayerController
    {
        [Header("Flags")]
        [NonSerialized] public Actor CurrentPlayer;
        [NonSerialized] public KtCamera ControllerCamera;

        public Vector3 ControllerAim;
        
        [Header("Yaw - Pitch")]
        public float YawSmoothTime   = 0.08f;
        public float PitchSmoothTime = 0.08f;
        public float PitchMin = -89f;
        public float PitchMax =  89f;
        public float Yaw   => _currentYaw;
        public float Pitch => _currentPitch;
        
        public float TargetPitch => _targetPitch;
        
        public float TargetYaw => _targetYaw;
        private float _currentYaw, _currentPitch;   
        private float _targetYaw,  _targetPitch; 
        private float _yawVel, _pitchVel;
            
        //Events
        public UnityAction<Actor> OnCurrentActorChanged;

        public void Tick(float dt)
        {
            _currentYaw = Mathf.SmoothDampAngle(
                _currentYaw, _targetYaw, ref _yawVel, YawSmoothTime, Mathf.Infinity, dt);

            _currentPitch = SmoothDampClamped(
                _currentPitch, _targetPitch, ref _pitchVel, PitchSmoothTime, dt, PitchMin, PitchMax);
        }
        
        #region Player Actor

        public void SetPlayerActor(Actor actor)
        {
            CurrentPlayer = actor;
            OnCurrentActorChanged?.Invoke(actor);
        }

        public void ClearPlayerActor()
        {
            CurrentPlayer = null;
            OnCurrentActorChanged?.Invoke(null);
        }

        public bool IsActorPlayer(Actor actor)
        {
            return actor == CurrentPlayer;
        }
        #endregion

        #region Yaw - Pitch
        public void AddYaw(float deltaYawDeg)
        {
            _targetYaw = Normalize360(_targetYaw + deltaYawDeg);
        }

        public void AddPitch(float deltaPitchDeg)
        {
            _targetPitch = Mathf.Clamp(_targetPitch + deltaPitchDeg, PitchMin, PitchMax);
        }

        public void SetYawImmediate(float yawDeg)
        {
            _targetYaw   = Normalize360(yawDeg);
            _currentYaw  = _targetYaw;
            _yawVel      = 0f;
        }

        public void SetPitchImmediate(float pitchDeg)
        {
            _targetPitch  = Mathf.Clamp(pitchDeg, PitchMin, PitchMax);
            _currentPitch = _targetPitch;
            _pitchVel     = 0f;
        }

        public Quaternion GetRotation()
        {
            return Quaternion.Euler(_currentPitch, _currentYaw, 0f);
        }

        public Vector3 GetLookDirection()
        {
            return GetRotation() * Vector3.forward;
        }

        // --- Helpers ---
        private static float Normalize360(float a) => (a % 360f + 360f) % 360f;

        private static float SmoothDampClamped(float current, float target, ref float vel, float smoothTime, float dt, float min, float max)
        {
            target = Mathf.Clamp(target, min, max);
            return Mathf.SmoothDamp(current, target, ref vel, smoothTime, Mathf.Infinity, dt);
        }
        #endregion

        #region Camera
        public Vector3 GetControllerDirection()
        {
            KtCamera camera = GetControllerCamera();
            if (camera == null) return Vector3.zero;
            return GetControllerCamera().transform.forward;
        }
        
        public KtCamera GetControllerCamera()
        {
            if (ControllerCamera == null)
            {
                return CameraManager.GetKtCamera();
            }
            return ControllerCamera;
        }
        #endregion

    }
}