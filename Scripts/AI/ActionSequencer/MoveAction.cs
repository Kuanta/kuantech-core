using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.ActionSequencer
{
    /// <summary>
    /// Movement action that moves the actor from a waypoint to another. Requires a movement component
    /// </summary>
    [Serializable]
    public class MoveAction : SequenceAction
    {
        [SerializeField] private Transform Waypoint;
        [SerializeField] private float Threshold = 0.1f;

        private Actor _actor;
        private MovementModule _mm;
        public override void Execute()
        {
            base.Execute();
            if(_actor == null) _actor = Parent.GetComponent<Actor>();
            _mm = _actor.MovementModule;
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
                _mm.Stop();
                return;
            }

            diffVec.y = 0;
            diffVec.Normalize();
            _mm.SetGlobalMovementVector(new Vector2(diffVec.x, diffVec.z));
            _mm.SetMaxSpeed(_actor.Stats.GetStat(StatTypes.MovementSpeed),_actor.Stats.GetStat(StatTypes.MovementSpeed));
        }
    }
}