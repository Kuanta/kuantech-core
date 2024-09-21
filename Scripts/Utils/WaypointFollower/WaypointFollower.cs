using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Utils
{
    public class WaypointFollower : MonoBehaviour
    {
        public class Waypoint
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public bool IsLocal;
            //public float Speed;
            public object UserData;
            
            //Event
            public Action<WaypointFollower> FollowerReachedWaypoint;
        }

        [Header("Properties")] [SerializeField]
        private float RotationLerpFactor = 10.0f;
        public float Speed = 10;
        
        [NonSerialized] public bool Moving;
        [NonSerialized] public Queue<Waypoint> Waypoints;
        [NonSerialized] public Waypoint CurrentWaypoint;

        public Action OnReachedFinalTarget;
        public Action<Waypoint> OnReachedWaypoint;

        #region Property Setters
        public void AddWaypoint(Waypoint newWaypoint)
        {
            if(Waypoints == null) Waypoints = new Queue<Waypoint>();
            if(CurrentWaypoint == null || Waypoints.Count == 0)
            {
                //No current waypoint
                CurrentWaypoint = newWaypoint;
                return;
            }
            //There is already a waypoint
            Waypoints.Enqueue(newWaypoint);

        }

        public void SetWaypoints(List<Waypoint> waypoints)
        {
            CurrentWaypoint = waypoints[0];
            Waypoints = new Queue<Waypoint>();
            foreach (var waypoint in waypoints)
            {
                Waypoints.Enqueue(waypoint);
            }
        }

        public void SetWaypoint(Waypoint waypoint)
        {
            Waypoints = new Queue<Waypoint>();
            Waypoints.Enqueue(waypoint);
            CurrentWaypoint = waypoint;
        }
        #endregion

        #region Controls

        public void FollowPath()
        {
            Moving = true;
        }

        public void Stop()
        {
            Moving = false;
        }
        #endregion
        
        /// <summary>
        /// For a single shot use
        /// </summary>
        /// <param name="worldPoint"></param>
        public void GoToWorldPoint(WorldPoint worldPoint, Action<WaypointFollower> onReachedAction=null)
        {
            Waypoint singleWaypoint = new Waypoint()
            {
                Position = worldPoint.GetTargetPosition(),
                Rotation = worldPoint.GetRotation(),
            };
            singleWaypoint.FollowerReachedWaypoint = onReachedAction;
            SetWaypoint(singleWaypoint);
            FollowPath();
        }
        private void Update()
        {
            if (!Moving) return;
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if(CurrentWaypoint == null) return;
            Vector3 error = CurrentWaypoint.IsLocal ? CurrentWaypoint.Position - transform.localPosition : CurrentWaypoint.Position - transform.position;
            float errorMag = error.magnitude;
            if(errorMag <= 0.01f)
            {
                OnReachedWaypoint?.Invoke(CurrentWaypoint);
                CurrentWaypoint.FollowerReachedWaypoint?.Invoke(this);
                SetNextWaypoint();
                return;
            }
            Vector3 direction = error / error.magnitude;
            if (direction.sqrMagnitude >= 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction),
                    Time.deltaTime * RotationLerpFactor);
            }
            Vector3 positionUpdate = direction * Mathf.Min(errorMag, Time.deltaTime * Speed);
            if(CurrentWaypoint.IsLocal)
            {
                transform.localPosition += positionUpdate;
            }else{
                transform.position += positionUpdate;
            }
        }

        private void SetNextWaypoint()
        {
            if(Waypoints == null) Waypoints = new Queue<Waypoint>();
            if(Waypoints.Count == 0)
            {
                //Snap to position
                transform.position = CurrentWaypoint.Position;
                transform.rotation = CurrentWaypoint.Rotation;
                CurrentWaypoint = null;
                OnReachedFinalTarget?.Invoke();
                return;
            }

            CurrentWaypoint = Waypoints.Dequeue();
        }

        public bool IsMoving()
        {
            return CurrentWaypoint != null;
        }
    }
}