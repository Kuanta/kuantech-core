using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class CameraRig : MonoBehaviour
    {
        public Transform LookTarget;

        public Vector3 Position => transform.position;

        public Quaternion GetLookRotation()
        {
            if (LookTarget == null) return transform.rotation;
            Vector3 dir = LookTarget.position - transform.position;
            if (dir.sqrMagnitude < 1e-6f) return transform.rotation;
            return Quaternion.LookRotation(dir, Vector3.up);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (LookTarget == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, LookTarget.position);
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            Gizmos.DrawWireSphere(LookTarget.position, 0.15f);
        }
#endif
    }
}
