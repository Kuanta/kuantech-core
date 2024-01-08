using System;
using Kuantech.AI;
using Kuantech.Core;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Kuantech.ArcadeIdle
{
    public class ArcadeIdleNpc : ArcadeIdleCharacter
    {
        public static float AGENT_START_ROTATE_DISTANCE = 0.2f;
        public static float AGENT_ANGLE_THRESHOLD = 1f;

        [Header("AI")]
        [SerializeField] private BTAgent BtAgent;
        [SerializeField] private NavMeshAgent NavMeshAgent;

        public ArcadeIdleVenue CurrentVenue;
        public EventHandler DespawnEvent;
        
        //Movement
        public float MaxSpeed = 5f; //Keep this since not every npc may have stats module
        public float AgentRotationSpeed = 120f;
        private WorldPoint _currentWorldPoint;
        private float _remainingSqrDistanceToTarget = 0;
        private float _remainingAngleToTarget = 0;

        //Targets (Used for behaviour tree)
        [NonSerialized] public VenueActor TargetVenueActor;
        [NonSerialized] public VenueInteractable TargetInteractable;
        [NonSerialized] public ResourceInventory TargetInventory;

        protected override void Update()
        {
            if (!Initialized) return;
            base.Update();

            //Check if actor has reached
            if (!StartedInteracting && AssignedSlot != null)
            {
                if (ReachedDestination())
                {
                    OnReachedToSlot();
                }
            }

            ArcadeIdleAnimator aiAnimator = GetModule<ArcadeIdleAnimator>();
            if(NavMeshAgent != null)
            {
                if (aiAnimator != null) aiAnimator.SetSpeed(NavMeshAgent.velocity.magnitude);
                if (NavMeshAgent.isStopped || _currentWorldPoint == null) return;
                NavMeshAgent.SetDestination(GetTargetPosition());
                CalculateRemainingDistanceAndRotation();
            }


            if (_remainingSqrDistanceToTarget <= AGENT_START_ROTATE_DISTANCE * AGENT_START_ROTATE_DISTANCE || NavMeshAgent == null)
            {
                Quaternion targetRotation = GetTargetRotation();
                transform.rotation =
                    Quaternion.Slerp(transform.rotation, targetRotation, AgentRotationSpeed * Time.deltaTime);
            }
        }

        protected override void UpdateStats()
        {
            if(StatsModule == null) return;
            base.UpdateStats();
            if(MovementSpeedAttribute == null) return;
            MaxSpeed = StatsModule.GetAttributeValue(MovementSpeedAttribute);
        }

        public void CalculateRemainingDistanceAndRotation()
        {   
            //Check if navmesh agent is null
            if(NavMeshAgent == null) {
                _remainingSqrDistanceToTarget = 0f;
                _remainingAngleToTarget = 0f;
                return;
            }
            _remainingSqrDistanceToTarget = Vector3.SqrMagnitude(transform.position - NavMeshAgent.destination);
            Quaternion targetRotation = GetTargetRotation();
            _remainingAngleToTarget = Quaternion.Angle(transform.rotation, targetRotation);
        }
        
        private Vector3 GetTargetPosition()
        {
            return _currentWorldPoint.Target != null ? _currentWorldPoint.Target.position + _currentWorldPoint.LocalPosition : _currentWorldPoint.Position;
        }

        private Quaternion GetTargetRotation()
        {
            return _currentWorldPoint.Target != null ? _currentWorldPoint.Target.rotation : _currentWorldPoint.Rotation;
        }
        public void Spawn(ArcadeIdleVenue venue, WorldPoint point, ActorState actorState = null)
        {
            CurrentVenue = venue;
            Initialize(actorState); //Initialize the actor
            BtAgent = GetModule<BTAgent>();
            if(BtAgent == null) return;
            BtAgent.RegisterVariable("Owner", this); //Register the actor
            BtAgent.StartAgent();
            if(point != null) WarpToPoint(point);
        }
        
        [Button("Despawn")]
        public void Despawn()
        {
            BtAgent.StopAgent();
            DespawnEvent?.Invoke(this, EventArgs.Empty);
            if (AssignedInteractable != null)
            {
                AssignedInteractable.RemoveActor(this);
            }
            Destroy(gameObject);
        }

        #region Movement & Navmesh
        public void SetDestination(Kuantech.Utils.WorldPoint point)
        {
            _currentWorldPoint = point;
            if(NavMeshAgent != null) NavMeshAgent.SetDestination(point.Position);
            CalculateRemainingDistanceAndRotation();
        }
        
        /// <summary>
        /// Toggles the movement of the navmesh agent
        /// </summary>
        /// <param name="toggle"></param>
        public void ToggleMovement(bool toggle)
        {
            if(NavMeshAgent == null) return;
            NavMeshAgent.isStopped = !toggle;
            NavMeshAgent.speed = toggle ? MaxSpeed : 0f;
        }

        public bool ReachedDestination()
        {
            float distThresh = AssignedSlot != null ? AssignedSlot.InteractionDistanceThreshold : AGENT_START_ROTATE_DISTANCE;
            if (_remainingSqrDistanceToTarget> distThresh * distThresh) return false;
            if(_remainingAngleToTarget > AGENT_ANGLE_THRESHOLD)
            {
                return false;
            }

            //WarpToTarget();
            return true;
        }

        public void WarpToTarget()
        {
            if(NavMeshAgent == null)
            {
                transform.position = GetTargetPosition();
            }else{
                bool warpSuccessful = NavMeshAgent.Warp(GetTargetPosition());
            }
            // Set the NPC's rotation to match the target's rotation
            transform.rotation = GetTargetRotation();
        }

        public void WarpToPoint(WorldPoint point)
        {
            if (NavMeshAgent == null)
            {
                transform.position = GetTargetPosition();
            }
            else
            {
                if(point.Target != null)
                {
                    NavMeshAgent.Warp(point.Target.position);
                }else{
                    NavMeshAgent.Warp(point.Position);
                }
            }
            transform.rotation = point.Target != null ? point.Target.rotation : point.Rotation;
        }

        #endregion
        
        #region Interactables

        public override void OnAssignedToSlot(InteractionSlot slot)
        {
            base.OnAssignedToSlot(slot);
            SetDestination(slot.GetTargetPoint());
            CalculateRemainingDistanceAndRotation();
            ToggleMovement(true);
        }

        #endregion

        public override void Reset()
        {
            base.Reset();
            ToggleMovement(false);
        }
    }
}