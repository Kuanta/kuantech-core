using UnityEngine;

namespace Kuantech.Core
{
    public class ChildOfTarget : MonoBehaviour
    {
        public Transform Target;

        private void Update()
        {
            if(Target == null) return;
            transform.position = Target.transform.position;
        }
    }
}