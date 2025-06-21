using UnityEngine;

namespace Kuantech.Utils
{
    public class PoolableComponent: MonoBehaviour
    {
        public GameObject CorrespondingPrefab;
        public bool InUse;

        private void OnDestroy()
        {
            if (InUse)
            {
               // Debug.LogError($"{gameObject.name} has been destroyed. It was created with pool");
            }
        }
    }
}