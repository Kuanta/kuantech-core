using System;
using Kuantech.Core;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.AI.ActionSequencer
{
    /// <summary>
    /// Movement action that moves the actor from a waypoint to another. Requires a movement component
    /// </summary>
    [Serializable]
    public class MoveAction : SequenceAction
    {
        [SerializeField] private Transform Waypoint;
        [SerializeField] private float Threshold = 0.1f;
        [SerializeField] private float MoveSpeed = 1;

        private Actor _actor;
        private MovementModule _mm;
        public override void Execute()
        {
            base.Execute();
            if(_actor == null) _actor = Parent.GetComponent<Actor>();
            _mm = _actor.GetModule<MovementModule>();
            if (_mm != null) return;
            IsComplete = true;
        }

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
                _mm.SetMovementVector(Vector3.zero);
                return;
            }

            diffVec.y = 0;
            diffVec.Normalize();
            _mm.SetMovementVector(new Vector2(diffVec.x, diffVec.z));
            _mm.SetSpeed(MoveSpeed);
            
        }
    }
}