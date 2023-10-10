using UnityEngine;
using Kuantech.Core;

namespace Kuantech.Core{
    
    public class SceneSubManagerContainer : MonoBehaviour {
        public SubManager[] GetSubManagers()
        {
            return transform.GetComponentsInChildren<SubManager>();
        }
    }
}