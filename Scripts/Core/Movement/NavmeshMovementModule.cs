using Kuantech.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace Kuantech.Core
{
    public class NavmeshMovementModule : ActorModule
    {
        [SerializeField] private NavMeshAgent NavMeshAgent;
        [Header("Knockback")]
        public float MinKnockbackForceRequired = 0.01f;
        
        private MovementModule _movementModule;
        
        public override void Initialize()
        {
            base.Initialize();
            Actor.OnActorRadiusSet += (object sender, float radius) =>
            {
                SetRadius(radius);
            };
        }
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _movementModule = Actor.GetModule<MovementModule>();
            if (_movementModule == null)
            {
                Debug.LogError("APP actor module requires movemente module");
            }
        }

        private float _lastEnsureTime;
        private void Update()
        {
            if (Actor == null || !Actor.IsAlive() || !Initialized || _movementModule.IsMovementLocked()) return;

            if (!IsOnNavmesh() && Time.time - _lastEnsureTime > 0.5f)
            {
                EnsureOnNavMesh(NavMeshAgent, 2.0f);
                _lastEnsureTime = Time.time;
                return;
            }
            UpdateSpeed();
            
            //Check knockback
            if(!CheckKnockback())
            {
                if (_movementModule != null)
                {
                    //Set animation vectors
                    Vector3 movementVector = !IsMoving() ? Vector3.zero : GetAgentVelocity().normalized;
                    _movementModule.SetMovementVector(movementVector);

                    if (IsMoving())
                    {
                        Actor.MotionVectorsHandler.SetTargetVector(movementVector);
                    }
                }
            }
        }

        #region Knockback
        protected virtual bool CheckKnockback()
        {
            Vector3 forceMove = Actor.MotionVectorsHandler.ForceMoveVector;
            if (forceMove.sqrMagnitude >= MinKnockbackForceRequired)
            {
                Stop();
                Actor.transform.position += forceMove * Time.deltaTime;
            }
            return false;
        }
        #endregion

        #region Commands
        
        /// <summary>
        /// Commands the agent to go to a point
        /// </summary>
        /// <param name="point"></param>
        public virtual void GoToWorldPoint(WorldPoint point)
        {
            GoToPosition(point.GetTargetPosition());
        }

        public virtual void GoToPosition(Vector3 position)
        {
            NavMeshAgent.SetDestination(position);
        }
        
        /// <summary>
        /// Sets actor speed
        /// </summary>
        /// <param name="speed"></param>
        public virtual void SetSpeed(float speed)
        {
            NavMeshAgent.speed = speed;
        }
        
        public virtual void SetRadius(float radius)
        {
            NavMeshAgent.radius = radius;
        }
        private void UpdateSpeed()
        {
            if (Actor == null || _movementModule == null) return;
            float speed = _movementModule.GetSpeed();
            SetSpeed(speed);
        }
        #endregion

        #region Checks
        public virtual bool IsMoving()
        {
            if (NavMeshAgent == null || !NavMeshAgent.isActiveAndEnabled || !IsOnNavmesh())
                return false;

            if (NavMeshAgent.pathPending) return true;
            if (!NavMeshAgent.hasPath) return false;

            return NavMeshAgent.remainingDistance > NavMeshAgent.stoppingDistance + 0.01f;
        }

        public virtual bool IsOnNavmesh()
        {
            return NavMeshAgent.isOnNavMesh;
        }
        public virtual Vector3 GetAgentVelocity()
        {
            if (NavMeshAgent == null || !NavMeshAgent.isActiveAndEnabled || !IsOnNavmesh())
                return Vector3.zero;

            return NavMeshAgent.velocity;
        }
        
        /// <summary>
        /// Checks whether the actor has reached its destination
        /// </summary>
        /// <returns></returns>
        public virtual bool ReachedDestination()
        {
            if (NavMeshAgent == null || !NavMeshAgent.isActiveAndEnabled || !IsOnNavmesh())
                return false;

            if (NavMeshAgent.pathPending) return false;
            if (NavMeshAgent.remainingDistance > NavMeshAgent.stoppingDistance) return false;

            return !NavMeshAgent.hasPath || NavMeshAgent.velocity.sqrMagnitude <= 0.0001f;
        }
        #endregion

        public virtual void Stop()
        {
            if (NavMeshAgent == null || !NavMeshAgent.isActiveAndEnabled) return;
            NavMeshAgent.isStopped = true;
            NavMeshAgent.enabled = false;
            Actor.MotionVectorsHandler.SetMovementVector(Vector3.zero);
        }
        
        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if (newState != ActorState.Spawned)
            {
                //On actor state changed to something else than spawned, stop the agent
                Stop();
            }
        }
        
        public static bool EnsureOnNavMesh(NavMeshAgent agent, float searchRadius = 2f)
        {
            if (agent == null) return false;

            if (!agent.enabled) agent.enabled = true; 


            if (agent.isOnNavMesh) return true;

            var t = agent.transform;
            if (UnityEngine.Physics.Raycast(t.position + Vector3.up * 2f, Vector3.down, out var hit, 10f,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                t.position = hit.point;
            }

            if (NavMesh.SamplePosition(t.position, out var navHit, searchRadius, NavMesh.AllAreas))
            {
                return agent.Warp(navHit.position);
            }

            Debug.LogWarning($"{t.name}: No navmesh near (r={searchRadius}m).");
            return false;
        }
    }
}