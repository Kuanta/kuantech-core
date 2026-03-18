using System;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public abstract class CrouchHandler
    {
        public abstract void OnCrouchStarted();
        public abstract void OnCrouchEnd();

        public abstract void Reset();
    }

    public class CapsuleColliderCrouchHandler : CrouchHandler
    {
        public CapsuleCollider CapsuleCollider;
        public float CrouchColliderHeight;
        public float StandupColliderHeight;
        public float ColliderHeightOffset;
        
        public override void OnCrouchStarted()
        {
            if (CapsuleCollider == null) return;
            CapsuleCollider.height = CrouchColliderHeight;
            Vector3 capsuleColliderCenter = CapsuleCollider.center;
            CapsuleCollider.center = new Vector3(capsuleColliderCenter.x,
                ColliderHeightOffset + CrouchColliderHeight * 0.5f, capsuleColliderCenter.z);
        }

        public override void OnCrouchEnd()
        {
            Reset();
        }

        public override void Reset()
        {
            if (CapsuleCollider == null) return;
            CapsuleCollider.height = StandupColliderHeight;
            Vector3 capsuleColliderCenter = CapsuleCollider.center;
            CapsuleCollider.center = new Vector3(capsuleColliderCenter.x,
                ColliderHeightOffset + StandupColliderHeight * 0.5f, capsuleColliderCenter.z);
        }
    }
}