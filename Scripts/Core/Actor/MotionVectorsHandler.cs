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
        [NonSerialized] public Vector3 ActorRightVector = Vector3.right;
        
        //Vectors
        [NonSerialized] public Vector3 MovementVector;
        [NonSerialized] public Vector3 ForceMoveVector = Vector3.zero; //Force move vector is used for knockback and other forceful movements        
        [NonSerialized] public Vector3 TargetVector = Vector3.zero;

        //Target
        public Transform TargetedObject;

        [NonSerialized] public Actor ParentActor;

        public MotionVectorsHandler(Actor actor, Vector3 actorForwardVector, Vector3 actorUpVector)
        {
            ParentActor = actor;
            ActorForwardVector = actorForwardVector;
            ActorUpVector = actorUpVector;
            ActorRightVector = Vector3.Cross(ActorUpVector, ActorForwardVector);
        }
        
        #region Movement Vectors

        /// <summary>
        /// Movement vector is the displacement vector
        /// </summary>
        /// <returns></returns>
        public Vector3 GetMovementVector()
        {
            return MovementVector;
        }

        public void SetMovementVector(Vector3 movementVector)
        {
            MovementVector = movementVector;
        }
        
        /// <summary>
        /// Sets the normalized vector
        /// </summary>
        /// <param name="side"></param>
        /// <param name="forward"></param>
        /// <param name="up"></param>
        public void SetMovementVector(float side, float forward, float up)
        {
            MovementVector = ActorForwardVector * forward + ActorRightVector * side + ActorUpVector * up;
            MovementVector.Normalize();
        }
        
        /// <summary>
        /// Get forward component of the movement vector
        /// </summary>
        /// <returns></returns>
        public float GetForwardMovement()
        {
            return Helpers.ProjectVector(MovementVector, ActorForwardVector).magnitude;
        }

        public float GetSideMovement()
        {
            return Helpers.ProjectVector(MovementVector, ActorRightVector).magnitude;
        }

        public float GetUpMovement()
        {
            return Helpers.ProjectVector(MovementVector, ActorUpVector).magnitude;
        }

        public void SetForwardMovement(float forward)
        {
            SetMovementVector(GetSideMovement(), forward, GetUpMovement());
        }

        public void SetSideMovement(float side)
        {
            SetMovementVector(side, GetForwardMovement(), GetUpMovement());
        }
        
        public void SetUpMovement(float up)
        {
            SetMovementVector(GetSideMovement(), GetForwardMovement(), up);
        }
        
        public void AddForceMovementVector(Vector3 forceMovementVector)
        {
            ForceMoveVector += forceMovementVector;
        }
        
        public void RemoveForceMovementVector(Vector3 forceMovementVector)
        {
            ForceMoveVector -= forceMovementVector;
            if (ForceMoveVector.sqrMagnitude < 0.01f)
            {
                ForceMoveVector = Vector3.zero; //Reset to zero if it is too small
            }
        }
        
        /// <summary>
        /// Returns local movement vector (side, up, forward)
        /// </summary>
        /// <returns></returns>
        public Vector3 GetLocalMovementVector()
        {
            Vector3 projectedMovement = ProjectToForwardPlane(GetMovementVector());
            Vector3 projectedTarget = ProjectToForwardPlane(TargetVector);

            if (projectedMovement.sqrMagnitude <= float.Epsilon)
            {
                return Vector3.zero;
            }
            
            //Get angle from projectedTarget to projectedMovement
            float angleDiff = Vector3.SignedAngle(projectedTarget, projectedMovement, ActorUpVector);
            return new Vector3(Mathf.Sin(angleDiff * Mathf.Deg2Rad), 0, Mathf.Cos(angleDiff * Mathf.Deg2Rad));
        }
        #endregion

        #region Target

        /// <summary>
        /// Target vector is the direction actor is facing
        /// </summary>
        /// <returns></returns>
        public Vector3 GetTargetVector()
        {
            //If target manager has a target...
            if (ParentActor != null && ParentActor.GetModule<TargetManager>() != null)
            {
                Actor target = ParentActor.GetModule<TargetManager>().GetCurrentTarget();
                if (target != null) TargetedObject = target.transform;
            }
            if (TargetedObject != null)
            {
                return (TargetedObject.position - ParentActor.transform.position).normalized;
            }
            if (TargetVector.sqrMagnitude > float.Epsilon)
            {
                return TargetVector;
            }
            return MovementVector; //same as movement
        }
 
        public void SetTargetObject(Transform targetObject)
        {
            TargetedObject = targetObject;
        }
        
        public void SetTargetVector(Vector3 targetVector)
        {
            TargetVector = targetVector;
        }
        #endregion


        
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
            return projected;
        }

        public void Reset()
        {
            MovementVector = Vector3.zero;
            ForceMoveVector = Vector3.zero;
            TargetVector = Vector3.zero;
            TargetedObject = null;
        }
    }
}