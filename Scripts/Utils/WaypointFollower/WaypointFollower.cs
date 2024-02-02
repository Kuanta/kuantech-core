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
            public bool IsLocal;
            public float Speed;
        }

        [Header("Properties")]
        [NonSerialized] public bool Moving;
        [NonSerialized] public Queue<Waypoint> Waypoints;
        [NonSerialized] public Waypoint CurrentWaypoint;

        public Action ReachedFinalTarget;

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
        #endregion


        private void Update()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if(CurrentWaypoint == null) return;
            Vector3 error = CurrentWaypoint.IsLocal ? CurrentWaypoint.Position - transform.localPosition : CurrentWaypoint.Position - transform.position;
            float errorMag = error.magnitude;
            if(errorMag <= 0.01f)
            {
                SetNextWaypoint();
                return;
            }
            Vector3 direction = error / error.magnitude;
            Vector3 positionUpdate = direction * Mathf.Min(errorMag, Time.deltaTime * CurrentWaypoint.Speed);
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
                CurrentWaypoint = null;
                ReachedFinalTarget?.Invoke();
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