using UnityEngine;

namespace Kuantech.Core.Utils
{
    [ExecuteInEditMode]
    public class ZoneGizmo : MonoBehaviour
    {
        [SerializeField] private Vector3 size = new Vector3(10, 1, 10);
        [SerializeField] private Vector3 offset = Vector3.zero;
        [SerializeField] private Color boundaryColor = Color.red;

        private void OnDrawGizmos()
        {
            Gizmos.color = boundaryColor;
            Gizmos.DrawWireCube(transform.position + offset, size);
        }
    }
}