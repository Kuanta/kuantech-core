using System;
using UnityEngine;

namespace Kuantech.ActionSequencer
{
    [Serializable]
    public class MoveAction : SequenceAction
    {
        [SerializeField] private Transform Waypoint;
        [SerializeField] private float Speed;
        [SerializeField] private float Threshold = 0.1f;
        
        public override void Update(float deltaTime)
        {
            if (Waypoint == null)
            {
                IsComplete = true;
                return;
            }
            //todo: Use movement module
            Vector3 diffVec = Waypoint.position - Parent.transform.position;
            if (diffVec.sqrMagnitude <= Threshold * Threshold)
            {
                IsComplete = true;
                return;
            }
            Parent.transform.position += diffVec.normalized * deltaTime * Speed;
        }
    }
}