using System;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public abstract class DashHandler
    {
        public abstract void HandleDash(MovementModule module, Vector3 direction);
        public virtual void OnDashStart(MovementModule module, Vector3 direction) { }
        public virtual void OnDashEnd(MovementModule module) { }
    }

    [Serializable]
    public class RigidbodyDashHandler : DashHandler
    {
        public bool TurnToDashDirection = true;
        public override void HandleDash(MovementModule module, Vector3 direction)
        {
            if (TurnToDashDirection)
            {
                module.Actor.MotionVectorsHandler.SetForceLookDirection(direction);
            }
            RigidbodyMovementModule rbModule = module.Actor.GetModule<RigidbodyMovementModule>();
            if (rbModule != null) rbModule.Dodge(direction, module.DashDuration, module.DashStrength);
        }

        public override void OnDashEnd(MovementModule module)
        {
            base.OnDashEnd(module);
            if (TurnToDashDirection)
            {
                module.Actor.MotionVectorsHandler.ClearForceLookDirection();
            }
        }
    }
}