using Kuantech.AI.Pathfinding;
using Kuantech.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.TowerDefense
{
    public class PathFollowerMovementModule : MovementModule
    {
        public PathFollower PathFollower;
        
        //Events
        public UnityAction OnReachedPathEndEvent;

        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            PathFollower.OnReachedPathEnd += OnReachedEnd;
        }
        public void SetPath(Path path)
        {
            PathFollower.SetPath(path);
        }
        
        public override void ModuleUpdate()
        {
            base.ModuleUpdate();

            if (PathFollower == null || PathFollower.IsMoving())
            {
                PathFollower.SetFollowSpeed(GetSpeed());
                
                //Update movement vectors
                Actor.MotionVectorsHandler.SetMovementVector(PathFollower.GetMovementVector());
            }
            else
            {
                SetMovementVector(Vector3.zero);
            }
        }
        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            PathFollower.Stop();
        }

        public override float GetSpeed()
        {
            if(PathFollower.CurrentFollowMethod == PathFollower.FollowMethod.WaypointFollower)
            {
                return base.GetSpeed(); //Handle knockback like usual
            }
            Vector3 currKnockback = GetKnockbackVector();
            float knockBackMag = currKnockback.magnitude;
            if (knockBackMag > float.Epsilon)
            {
                return -1 * knockBackMag;
            }

            return base.GetSpeed();
        }
        #region Path

        public void SetOnPath(Path path)
        {
            PathFollower.FollowPath(path);
        }

        public void Stop()
        {
            PathFollower.Stop();
        }

        #endregion
        #region Event Handlers

        private void OnReachedEnd()
        {
            OnReachedPathEndEvent?.Invoke();
        }

        #endregion
    }
}