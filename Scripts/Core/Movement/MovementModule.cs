using System.Collections;
using System.Collections.Generic;
using Kuantech.Core.Utils;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Core
{
    public class MovementModule : ActorModule
    {
        [Header("Speed")]
        public AttributeAsset SpeedAttribute;
        [Tooltip("Fallback speed if speed attribute cant get")]
        public float Speed = 1f;
        
        //Lock
        public LockVariable MovementLock;

        #region Movement Vector
        /// <summary>
        /// Sets the actor movement vector
        /// </summary>
        /// <param name="movementVector"></param>
        public void SetMovementVector(Vector3 movementVector)
        {
            Actor.MotionVectorsHandler.SetMovementVector(movementVector);
        }
        
        /// <summary>
        /// Gets the movement vector
        /// </summary>
        public Vector3 GetMovementVector()
        {
            return Actor.MotionVectorsHandler.GetMovementVector();
        }
        #endregion
        
        #region Speed
        /// <summary>
        /// Gets the actor speed
        /// </summary>
        /// <returns></returns>
        public virtual float GetSpeed()
        {
            if (IsMovementLocked()) return 0f;
            StatsModule sm = Actor.GetModule<StatsModule>();
            if (sm == null) return Speed;
            Attribute speedAtt = sm.GetAttribute(SpeedAttribute);
            if (speedAtt == null) return Speed;
            return speedAtt.GetValue(sm.GetActorLevel());
        }
        #endregion
        
        #region Locks

        public bool IsMovementLocked()
        {
            if (MovementLock != null && MovementLock.IsLocked()) return true;
            return false;
        }
        
        /// <summary>
        /// Locks the movement
        /// </summary>
        /// <param name="locker"></param>
        public void Lock(object locker)
        {
            if (MovementLock == null)
            {
                MovementLock = new LockVariable();
            }
            MovementLock.Lock(locker);
            if (MovementLock.IsLocked())
            {
                MovementLocked();
            }
        }
        
        /// <summary>
        /// Unlocks the movement
        /// </summary>
        /// <param name="locker"></param>
        public void Unlock(object locker)
        {
            if (!IsMovementLocked()) return;
            bool alreadyUnlocked = !MovementLock.IsLocked();
            if (alreadyUnlocked) return;
            
            MovementLock.Unlock(locker);
            if(!MovementLock.IsLocked())
                MovementUnlocked();
            else
                MovementLocked();
        }
        protected virtual void MovementLocked()
        {
            
        }
        
        protected virtual void MovementUnlocked()
        {
            
        }
        #endregion
        
        #region Knockback
        private HashSet<IEnumerator> _knockbackRoutines = new HashSet<IEnumerator>();
        
        public void Knockback(Vector3 direction, float knockback, float knockbackTime)
        {
            if (knockback == 0 || knockbackTime == 0) return;
            IEnumerator routine = KnockbackRoutine(direction, knockback, knockbackTime);
            _knockbackRoutines.Add(routine);
            StartCoroutine(routine);
        }
        
        private IEnumerator KnockbackRoutine(Vector3 direction, float knockback, float knockbackTime)
        {
            direction.Normalize();
            direction *= knockback;
            AddForceMoveVector(direction);
            yield return new WaitForSeconds(knockbackTime);
            RemoveForceMoveVector(direction);
        }
        
        protected virtual void AddForceMoveVector(Vector3 forceMoveVector)
        {
            Actor.MotionVectorsHandler.AddForceMovementVector(forceMoveVector);
        }
        
        protected virtual void RemoveForceMoveVector(Vector3 forceMoveVector)
        {
            Actor.MotionVectorsHandler.RemoveForceMovementVector(forceMoveVector);
        }

        public Vector3 GetKnockbackVector()
        {
            return Actor.MotionVectorsHandler.ForceMoveVector;
        }
        #endregion

        public override void Reset()
        {
            base.Reset();
            
            //Zero out the knockback vectors
            foreach (var routine in _knockbackRoutines)
            {
                StopCoroutine(routine);
            }
            _knockbackRoutines.Clear();
            Actor.MotionVectorsHandler.ForceMoveVector = Vector3.zero;
        }
    }
}