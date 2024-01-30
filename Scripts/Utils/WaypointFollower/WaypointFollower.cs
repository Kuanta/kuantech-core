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
        }

        [Header("Properties")]
        public float Speed;
        [NonSerialized] public bool Moving;
        [NonSerialized] public Queue<Waypoint> Waypoints;
        [NonSerialized] public Waypoint CurrentWaypoint;

        public Action ReachedFinalTarget;

        #region Property Setters
        public void SetSpeed(float speed)
        {
            Speed = speed;
        }
        public void AddWaypoint(Waypoint newWaypoint)
        {
            Waypoints.Enqueue(newWaypoint);
        }
        #endregion


        private void Update()
        {

        }

        private void UpdatePosition()
        {
            if(CurrentWaypoint == null) return;
            Vector3 error = CurrentWaypoint.Position - transform.position;
            float errorMag = error.magnitude;
            if(errorMag <= 0.01f)
            {
                SetNextWaypoint();
                return;
            }
            Vector3 direction = error / error.magnitude;
            Vector3 positionUpdate = direction * Mathf.Min(errorMag, Time.deltaTime * Speed);
            transform.position += positionUpdate;
        }

        private void SetNextWaypoint()
        {
            if(Waypoints.Count == 0)
            {
                ReachedFinalTarget?.Invoke();
                return;
            }

            CurrentWaypoint = Waypoints.Dequeue();
        }

    }
}