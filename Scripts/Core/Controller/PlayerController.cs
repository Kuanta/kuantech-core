using System;
using Kuantech.Core.Camera;
using UnityEngine;

namespace Kuantech.Core.Controller
{
    /// <summary>
    /// Player controller, handles the 
    /// </summary>
    public class PlayerController
    {
        [Header("Flags")]
        
        [NonSerialized] public Actor CurrentPlayer;
        [NonSerialized] public KtCamera ControllerCamera;

        public Vector3 ControllerAim;
        
        #region Player Actor

        public void SetPlayerActor(Actor actor)
        {
            CurrentPlayer = actor;
        }

        public void ClearPlayerActor()
        {
            CurrentPlayer = null;
        }

        public bool IsActorPlayer(Actor actor)
        {
            return actor == CurrentPlayer;
        }
        #endregion

        #region Yaw - Pitch
        private float _yaw;
        private float _pitch;

        public float Yaw => _yaw;
        public float Pitch => _pitch;

        public void SetYaw(float yaw)
        {
            _yaw = yaw;
        }

        public void SetPitch(float pitch)
        {
            _pitch = Mathf.Clamp(pitch, -80f, 80f); // limitler sende
        }

        public Quaternion GetRotation()
        {
            return Quaternion.Euler(_pitch, _yaw, 0f);
        }

        public Vector3 GetLookDirection()
        {
            return GetRotation() * Vector3.forward;
        }

        #endregion

        #region Inputs

        public void AddYaw(float yaw)
        {
            SetYaw(Yaw  + yaw);
        }
        
        public void AddPitch(float pitch)
        {
            SetPitch(Mathf.Clamp(Pitch + pitch, -89.0f, 89.0f));
        }
        
        #endregion
    }
}