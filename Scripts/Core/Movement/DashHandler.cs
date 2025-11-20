using System.Collections;
using UnityEngine;

namespace Kuantech.Core
{
    public abstract class DashHandler
    {
        public abstract void HandleDash(MovementModule module);
    }
    
    public class RigidbodyDashHandler : DashHandler
    {
        public override void HandleDash(MovementModule module)
        {
            
            module.Actor.StartCoroutine(DashCoroutine(module));
        }

        private IEnumerator DashCoroutine(MovementModule module)
        {
            Vector3 targetVector = module.Actor.MotionVectorsHandler.GetTargetVector();
            Vector3 dashForce = targetVector * module.DashStrength;
            module.Actor.MotionVectorsHandler.AddForceMovementVector(dashForce);
            yield return new WaitForSeconds(module.DashDuration);
            module.Actor.MotionVectorsHandler.RemoveForceMovementVector(dashForce);
        }
    }
}