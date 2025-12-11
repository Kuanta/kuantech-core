using UnityEngine;

namespace Kuantech.Core
{
    public class AimHandler : ActorModule
    {
        [SerializeField] private Rigidbody Rigidbody;
        [SerializeField] private bool PrioritizeMovementForTargetVector;
        [SerializeField] private float rotateSpeedDegPerSec = 720f;
        [SerializeField] private Transform Anchor;
        private Vector3 _targetAimVector;
        Quaternion _targetRot = Quaternion.identity;

        public override void ModuleLateUpdate()
        {
            if (!Actor.IsAlive()) return;
            _targetAimVector = Actor.MotionVectorsHandler.GetTargetVector(PrioritizeMovementForTargetVector);
            Transform t = Actor.transform;
            if (_targetAimVector.sqrMagnitude < 1e-8f)
                return;
            
            _targetRot = DirectionToRotation(transform, _targetAimVector);
            if(Rigidbody == null)
            {
                t.rotation = Quaternion.RotateTowards(t.rotation, _targetRot, rotateSpeedDegPerSec * Time.deltaTime);
            }
            else
            {
                var next = Quaternion.RotateTowards(
                    Rigidbody.rotation, _targetRot, rotateSpeedDegPerSec * Time.fixedDeltaTime);
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
            
            if(Rigidbody == null)
            {
                transform.rotation = rot;
            }
            else
            {
                Rigidbody.rotation = rot;
            }
        }
    }
}