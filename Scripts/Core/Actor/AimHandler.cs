using System;
using UnityEngine;

namespace Kuantech.Core
{
    public class AimHandler : ActorModule
    {
        [SerializeField] private Rigidbody Rigidbody;
        [SerializeField] private bool PrioritizeMovementForTargetVector;
        [SerializeField] private float rotateSpeedDegPerSec = 720f;
        private Vector3 _targetAimVector;
        Quaternion _targetRot = Quaternion.identity;

        public override void ModuleLateUpdate()
        {
            if (!Actor.IsAlive()) return;
            _targetAimVector = Actor.MotionVectorsHandler.GetTargetVector(PrioritizeMovementForTargetVector);
            Transform t = Actor.transform;
            if (_targetAimVector.sqrMagnitude < 1e-8f)
                return;
            Vector3 axis = Actor.ActorUpVector;
            
            Vector3 projected = Vector3.ProjectOnPlane(_targetAimVector, axis);

            if (projected.sqrMagnitude < 1e-8f)
            {
                projected = Vector3.ProjectOnPlane(t.forward, axis);
                if (projected.sqrMagnitude < 1e-8f)
                {
                    projected = Vector3.Cross(axis, t.right);
                }
            }

            projected.Normalize();

            _targetRot = Quaternion.LookRotation(projected, axis);
     
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
    }
}