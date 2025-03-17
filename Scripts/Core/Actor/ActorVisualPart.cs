using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
   

    /// <summary>
    /// Visual parts like hair, beard, naked body etc.
    /// </summary>
    public class ActorVisualPart : MonoBehaviour
    {
        public List<GameObject> ConnectedObjects; //For cases where

        public void Toggle(bool toggle)
        {
            if (!ConnectedObjects.IsNullOrEmpty())
            {
                foreach (GameObject obj in ConnectedObjects)
                {
                    obj.SetActive(toggle);
                }
            }
            gameObject.SetActive(toggle);
        }
    }
}