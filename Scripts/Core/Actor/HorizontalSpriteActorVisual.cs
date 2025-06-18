using UnityEngine;

namespace Kuantech.Core
{
    public class HorizontalSpriteActorVisual : ActorVisual
    {
        public Vector3 FacingDirection = Vector3.forward;
        public float AimDirectionThreshold = 0.1f;
        private Vector3 _lastDirection;
        
        private void Update()
        {
            if (ParentActor == null) return;

            Vector3 aimDirection = ParentActor.MotionVectorsHandler.GetTargetVector();
            if (aimDirection.sqrMagnitude > AimDirectionThreshold)
            {
                Debug.Log("Aim Direction "+" "+aimDirection);

                float dot = Vector3.Dot(aimDirection, FacingDirection);
                Vector3 localScale = transform.localScale;

                if (dot >= 0)
                {
                    localScale.x = Mathf.Abs(localScale.x);
                }
                else
                {
                    localScale.x = Mathf.Abs(localScale.x) * -1;
                }
                transform.localScale = localScale;
            }
        }
    }
}