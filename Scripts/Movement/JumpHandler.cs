using System;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public abstract class JumpHandler
    {
        public abstract void HandleJump(MovementModule module, Vector3 jumpVector);
    }

    [Serializable]
    public class RigidbodyJumpHandler : JumpHandler
    {
        public override void HandleJump(MovementModule module, Vector3 jumpVector)
        {
            Rigidbody rb = module.Actor.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(jumpVector, ForceMode.Impulse);
        }
    }
}
