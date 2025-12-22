using UnityEngine;

namespace Kuantech.Core.UI
{
    public class DiscreteFillChunk : MonoBehaviour
    {
        public GameObject OnObject;
        public GameObject OffObject;

        public void Toggle(bool toggle)
        {
            OnObject.SetActive(toggle);
            OffObject.SetActive(!toggle);
        }
    }
}