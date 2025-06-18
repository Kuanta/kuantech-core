using Kuantech.Core;
using Kuantech.Rpg;
using Pathfinding;
using UnityEngine;

namespace Kuantech.ThirdParty.AStarPathfindingProject
{
    /// <summary>
    /// This module is used to add the A* Pathfinding Project to the game.
    /// </summary>
    public class APPActorModule : ActorModule
    {
        public FollowerEntity AIAgent;
        public AttributeAsset MovementSpeedAttribute;
        
        private void Update()
        {
            if (Actor == null || !Actor.IsAlive()) return;
            
            UpdateSpeed();
            
            //Go to clicked point
            if (Input.GetMouseButtonDown(0) && Actor.IsAlive())
            {
                Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                worldPoint.z = 0;
                GoToPoint(worldPoint);
            }

            if (AIAgent != null)
            {
                //Set animation vectors
                if (AIAgent.isStopped)
                {
                    Actor.MotionVectorsHandler.SetMovementVector(Vector3.zero);
                }
                else
                {
                    Actor.MotionVectorsHandler.SetMovementVector(AIAgent.velocity.normalized);
                }
            }
        }

        private void UpdateSpeed()
        {
            if (Actor == null) return;
            StatsModule sm = Actor.GetModule<StatsModule>();
            if(sm == null || MovementSpeedAttribute == null) return;
            float speed = sm.GetAttributeValue(MovementSpeedAttribute);
            SetSpeed(speed);
        }
        public void GoToPoint(Vector3 destination)
        {
            AIAgent.isStopped = false;
            AIAgent.destination = destination;
            AIAgent.SearchPath();
        }

        public void SetSpeed(float speed)
        {
            AIAgent.maxSpeed = speed;
        }
        
        public void Stop()
        {
            AIAgent.isStopped = true;
            AIAgent.destination = AIAgent.transform.position;
        }

        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if (newState == ActorState.Dead)
            {
                Stop();
            }
        }
    }
}