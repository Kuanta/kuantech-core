using System;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Most basic camera positioner component
    /// </summary>
    public class CameraFollower : MonoBehaviour
    {
        public GameObject CameraObject;
        public CameraTarget CameraTarget;

        [Header("Camera Properties")] 
        public float PositionLerpSpeed = 5f;
        public float RotationLerpSpeed = 5f;
        
        
        //Runtime
        [NonSerialized] protected Vector3 TargetPosition;
        [NonSerialized] protected Vector3 TargetDirection;
        
        protected virtual void LateUpdate()
        {
            if (CameraTarget == null) return;
            SetTargetPosition();
            SetTargetDirection();
        
            UpdatePosition(TargetPosition);
            UpdateRotation(TargetDirection);
        }
        
        /// <summary>
        /// Snaps to target
        /// </summary>
        public void SnapToTarget()
        {
            if (CameraTarget != null)
            {
                SetTargetPosition();
                SetTargetDirection();
            }
            
            SetPosition(TargetPosition);
            SetRotation(Quaternion.LookRotation(TargetDirection));
        }
        
        /// <summary>
        /// Sets the position immediately
        /// </summary>
        /// <param name="position"></param>
        public void SetPosition(Vector3 position)
        {
            TargetPosition = position;
            _SetPosition(position);
        }
        
        /// <summary>
        /// Sets the rotation immediately
        /// </summary>
        /// <param name="rotation"></param>
        public void SetRotation(Quaternion rotation)
        {
            TargetDirection = rotation * Vector3.forward;
            _SetRotation(rotation);
        }
        
        public virtual void SetTargetPosition()
        {
            if (CameraTarget != null)
            {
                TargetPosition = CameraTarget.GetTargetPosition();
            }
        }
        
        public virtual void SetTargetDirection()
        {
            if (CameraTarget != null)
            {
                TargetDirection  = CameraTarget.GetTargetRotation() * Vector3.forward;
            }
        }
        
        protected virtual void UpdatePosition(Vector3 targetPosition)
        {
            _SetPosition(Vector3.Lerp(_GetPostition(), targetPosition, PositionLerpSpeed * Time.deltaTime));
        }

        protected virtual void UpdateRotation(Vector3 targetDirection)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDirection);
            SetRotation(Quaternion.Slerp(_GetRotation(), targetRot, Time.deltaTime * RotationLerpSpeed));
        }

        #region Setter & Getter

        private Vector3 _GetPostition()
        {
            if (CameraObject != null) return CameraObject.transform.position;
            return transform.position;
        }

        private Quaternion _GetRotation()
        {
            if (CameraObject != null) return CameraObject.transform.rotation;
            return transform.rotation;
        }

        private void _SetPosition(Vector3 position)
        {
            if (CameraObject != null)
            {
                CameraObject.transform.position = position;
            }
            else
            {
                transform.position = position;
            }
        }

        private void _SetRotation(Quaternion rotation)
        {
            if (CameraObject != null) CameraObject.transform.rotation = rotation;
            else transform.rotation = rotation;
        }

        #endregion
    
    }
}