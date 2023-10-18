using UnityEngine;
using Kuantech.Core;
using System.Collections.Generic;

namespace Kuantech.Core{
    
    public class SceneSubManagerContainer : MonoBehaviour {
        public SubManager[] GetSubManagers()
        {
            return transform.GetComponentsInChildren<SubManager>();
        }
        public List<GameObject> ManagerDependentObjects;

        private void Awake()
        {
            foreach(var obj in ManagerDependentObjects)
            {
                obj.SetActive(false);
            }
        }

        /// <summary>
        /// Activates game objects that require submanagers. 
        /// Useful for cases where Update method of these objects require aceess to submanagers.
        /// </summary>
        public void ActivateManagerDependentSceneObjects()
        {
            foreach (var obj in ManagerDependentObjects)
            {
                obj.SetActive(true);
            }
        }
    }
}