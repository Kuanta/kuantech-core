using System;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class MotionVectorsHandler
    {
        [Header("Frame")] 
        public Vector3 ActorUpVector = Vector3.up;
        public Vector3 ActorForwardVector = Vector3.forward;

        public Transform TargetedObject;
        public Vector3 MovementVector;
        public Vector3 TargetVector;

        [NonSerialized] public Actor ParentActor;
        
        /// <summary>
        /// Movement vector is the displacement vector
        /// </summary>
        /// <returns></returns>
        public Vector3 GetMovementVector()
        {
            return MovementVector;
        }
        
        /// <summary>
        /// Target vector is the direction actor is facing
        /// </summary>
        /// <returns></returns>
        public Vector3 GetTargetVector()
        {
            if (TargetedObject != null)
            {
                return (TargetedObject.position - ParentActor.transform.position).normalized;
            }
            if (TargetVector.sqrMagnitude <= 0.001f)
            {
                return ParentActor.transform.forward;
            }

            return ActorForwardVector;
        }
        
        public void SetMovementVector(Vector3 movementVector)
        {
            MovementVector = movementVector;
        }

        public void SetTargetVector(Vector3 targetVector)
        {
            
        }

        public Vector3 GetLocalMovementVector()
        {
            
        }
        
        /// <summary>
        /// Projects a vector to the forward plane of the actor
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public Vector3 ProjectToForwardPlane(Vector3 vector)
        {
            Vector3 up = ActorUpVector;
            Vector3 forward = ActorForwardVector;

            Vector3 projected = Helpers.ProjectVectorOnPlane(vector, up, Vector3.zero);
            
        }
    }
}