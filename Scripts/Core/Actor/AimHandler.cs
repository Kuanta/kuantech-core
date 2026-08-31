using Kuantech.Core.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class AimHandler : ActorModule
    {
        [SerializeField] private Rigidbody Rigidbody;
        [SerializeField] private bool PrioritizeMovementForTargetVector;
        [SerializeField] private float rotateSpeedDegPerSec = 720f;
        [SerializeField] private Transform Anchor;
        [Tooltip("For multiplayer, if rotation is synced turn this off")]
        [SerializeField] private bool RotateOnClient = true;
        [Tooltip("Snaps straight to the target rotation instead of RotateTowards-ing at rotateSpeedDegPerSec. " +
                 "First-person needs this: the camera isn't a child of the actor the way it would be in Unreal, " +
                 "so the body has to match the camera's yaw exactly, every frame, with zero catch-up lag — " +
                 "PlayerModule toggles this when switching view modes.")]
        public bool InstantRotation = false;
        private Vector3 _targetAimVector;
        Quaternion _targetRot = Quaternion.identity;

        public LockKey RotationLockKey;

        private LockModule _lockModule;

        // public void LockRotation(object locker) => RotationLock.Lock(locker);
        // public void UnlockRotation(object locker) => RotationLock.Unlock(locker);

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _lockModule = Actor.GetModule<LockModule>();    
        }

        public override void ModuleLateUpdate(float deltaTime)
        {
            if (!Actor.IsAlive()) return;
            if(!RotateOnClient && !Actor.IsServer) return;
            if (IsRotationLocked()) return;
            _targetAimVector = Actor.MotionVectorsHandler.GetTargetVector(PrioritizeMovementForTargetVector);
            Transform t = Actor.transform;
            if (_targetAimVector.sqrMagnitude < 1e-8f)
                return;
            
            _targetRot = DirectionToRotation(transform, _targetAimVector);
            if(Rigidbody == null || Rigidbody.isKinematic)
            {
                t.rotation = InstantRotation
                    ? _targetRot
                    : Quaternion.RotateTowards(t.rotation, _targetRot, rotateSpeedDegPerSec * deltaTime);
            }
            else
            {
                var next = InstantRotation
                    ? _targetRot
                    : Quaternion.RotateTowards(Rigidbody.rotation, _targetRot, rotateSpeedDegPerSec * deltaTime);
                Rigidbody.MoveRotation(next);
            }
        }
        
        private Quaternion DirectionToRotation(Transform anchor, Vector3 direction)
        {
            Vector3 axis = Actor.ActorUpVector;
            
            Vector3 projected = Vector3.ProjectOnPlane(direction, axis);
            if (projected.sqrMagnitude < 1e-8f)
            {
                projected = Vector3.ProjectOnPlane(anchor.forward, axis);
                if (projected.sqrMagnitude < 1e-8f)
                {
                    projected = Vector3.Cross(axis, anchor.right);
                }
            }

            projected.Normalize();
            return Quaternion.LookRotation(projected, axis);
        }
        
        //Rotates immediately
        public void SetDirection(Vector3 direction)
        {
            Quaternion rot = DirectionToRotation(transform, direction);
            _targetAimVector = direction;

            transform.rotation = rot;
            if(Rigidbody != null)
            {
                Rigidbody.rotation = rot;
            }
        }

        #region Locks
        public bool IsRotationLocked()
        {
            if(_lockModule == null || RotationLockKey == null) return false;
            return _lockModule.IsLocked(RotationLockKey);
        }

        public void LockRotation(object locker)
        {
            if (_lockModule == null)     { Debug.LogWarning($"[AimHandler] {Actor.name}: LockModule is null");      return; }
            if (RotationLockKey == null) { Debug.LogWarning($"[AimHandler] {Actor.name}: RotationLockKey is null"); return; }
            _lockModule.Lock(RotationLockKey, locker);
        }

        public void UnlockRotation(object locker)
        {
            if (_lockModule == null) return;
            _lockModule.Unlock(RotationLockKey, locker);
        }
        #endregion
    }
}