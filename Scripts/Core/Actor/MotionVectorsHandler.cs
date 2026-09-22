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
        [NonSerialized] public float MovementMultiplier = 1f;

        //Target
        public Transform TargetedObject;
        [NonSerialized] public Vector3 ForceLookDirection = Vector3.zero;

        [NonSerialized] public Actor ParentActor;

        public Action<Vector3> OnMovementVectorChanged;
        public Action<Vector3> OnTargetVectorChanged;
        public Action<float> OnMovementMultiplierChanged;
        public Action<Transform> OnTargetChanged;
        

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
            OnMovementVectorChanged?.Invoke(movementVector);
        }
        
        public float GetMovementMultiplier()
        {
            return MovementMultiplier;
        }
        
        public void SetMovementMultiplier(float multiplier)
        {
            MovementMultiplier = multiplier;
            OnMovementMultiplierChanged?.Invoke(multiplier);
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
            Vector3 projectedTarget = ProjectToForwardPlane(GetTargetVector());

            if (projectedMovement.sqrMagnitude <= float.Epsilon)
            {
                return Vector3.zero;
            }
            //Get angle from projectedTarget to projectedMovement
            float angleDiff = Vector3.SignedAngle(projectedTarget, projectedMovement, ActorUpVector);
            Vector3 localMovement = new Vector3(Mathf.Sin(angleDiff * Mathf.Deg2Rad), 0, Mathf.Cos(angleDiff * Mathf.Deg2Rad));
            return localMovement;
        }
        #endregion

        #region Target

        /// <summary>
        /// Target vector is the direction actor is facing
        /// </summary>
        /// <returns></returns>
        public Vector3 GetTargetVector(bool prioritizeMovementOverTarget = false)
        {
            
            if (ForceLookDirection.sqrMagnitude > 0.1f)
                return ForceLookDirection;

            //Check movement priority
            if (prioritizeMovementOverTarget && MovementVector.sqrMagnitude > float.Epsilon)
            {
                return MovementVector;
            }

            //Buisness as usual
            if (TargetedObject != null)
            {
                return (TargetedObject.position - ParentActor.transform.position).normalized;
            }
            if (TargetVector.sqrMagnitude > float.Epsilon)
            {
                return TargetVector;
            }

            if (MovementVector.sqrMagnitude > float.Epsilon)
            {
                return MovementVector;
            }

            return ParentActor.transform.forward;
        }

        /// <summary>
        /// The direction this actor is DELIBERATELY facing, or zero when nothing is aiming it.
        ///
        /// Same ranking as GetTargetVector minus its fallbacks, and the fallbacks are the whole point of
        /// having a second method: the movement vector and transform.forward are things any peer can work
        /// out for itself from state it already has, so replicating them is waste at best. Replicating
        /// transform.forward is worse than waste -- it freezes a remote actor's facing at whatever angle
        /// the sample happened to catch, instead of letting it fall back to its own movement direction.
        /// </summary>
        public Vector3 GetAimDirection()
        {
            if (ForceLookDirection.sqrMagnitude > 0.1f) return ForceLookDirection;
            if (TargetedObject != null)
                return (TargetedObject.position - ParentActor.transform.position).normalized;
            return TargetVector;
        }

        public void SetTargetObject(Transform targetObject)
        {
            TargetedObject = targetObject;
            OnTargetChanged?.Invoke(targetObject);
        }
        
        public void SetTargetVector(Vector3 targetVector)
        {
            TargetVector = targetVector;
            OnTargetVectorChanged?.Invoke(targetVector);
        }

        public void ClearTargetVector()
        {
            SetTargetVector(Vector3.zero);
        }
        public void ClearTargetObject()
        {
            SetTargetObject(null);
        }

        public void SetForceLookDirection(Vector3 direction)
        {
            ForceLookDirection = direction;
        }

        public void ClearForceLookDirection()
        {
            ForceLookDirection = Vector3.zero;
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
            return Vector3.ProjectOnPlane(vector, up).normalized;
        }

        public void Reset()
        {
            MovementVector = Vector3.zero;
            ForceMoveVector = Vector3.zero;
            TargetVector = Vector3.zero;
            TargetedObject = null;
            ForceLookDirection = Vector3.zero;
        }
    }
}