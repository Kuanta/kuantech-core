using System;
using Kuantech.Core.Combat;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public abstract class DashHandler
    {
        public bool GrantInvulnerability = false;

        public virtual bool CanDash(Actor actor)
        {
            return true;
        }

        public abstract void HandleDash(MovementModule module, Vector3 direction);

        public virtual void OnDashStart(MovementModule module, Vector3 direction)
        {
            if (GrantInvulnerability)
                module.Actor.GetModule<HealthcareModule>()?.SetInvulnerable(this);
        }

        public virtual void OnDashEnd(MovementModule module)
        {
            if (GrantInvulnerability)
                module.Actor.GetModule<HealthcareModule>()?.ClearInvulnerable(this);
        }
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

        public override void OnDashStart(MovementModule module, Vector3 direction)
        {
            base.OnDashStart(module, direction);
        }

        public override void OnDashEnd(MovementModule module)
        {
            base.OnDashEnd(module);
            if (TurnToDashDirection)
                module.Actor.MotionVectorsHandler.ClearForceLookDirection();
        }
    }
}