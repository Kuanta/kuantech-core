using UnityEngine;

namespace Kuantech.Core
{
    public class AimHandler : ActorModule
    {
        [SerializeField] private float rotateSpeedDegPerSec = 720f;
        public override void ModuleUpdate()
        {
            base.ModuleUpdate();
            if (!Actor.IsAlive()) return;
            Vector3 aimVector = Actor.MotionVectorsHandler.GetTargetVector();
            Transform t = Actor.transform;
            if (aimVector.sqrMagnitude < 1e-8f)
                return;
            Quaternion targetRot;
            Vector3 axis = Actor.ActorUpVector;
            
            Vector3 projected = Vector3.ProjectOnPlane(aimVector, axis);

            // Projeksiyon neredeyse sıfırsa güvenli geri dönüş:
            if (projected.sqrMagnitude < 1e-8f)
            {
                // mevcut forward'ı projekte etmeyi dene
                projected = Vector3.ProjectOnPlane(t.forward, axis);
                if (projected.sqrMagnitude < 1e-8f)
                {
                    // hâlâ kötü ise axis'e dik herhangi bir vektör seç (sağ vektörden türet)
                    projected = Vector3.Cross(axis, t.right);
                }
            }

            projected.Normalize();

            // Up olarak ekseni ver → bu, dönmeyi söz konusu eksen etrafında kilitler (yaw-only vb.)
            targetRot = Quaternion.LookRotation(projected, axis);
            t.rotation = Quaternion.RotateTowards(t.rotation, targetRot, rotateSpeedDegPerSec * Time.deltaTime);
        }
    }
}