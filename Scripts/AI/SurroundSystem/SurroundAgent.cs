using System;
using UnityEngine;

namespace Kuantech.SurroundSystem
{
    [Serializable]
    public class SurroundAgent : MonoBehaviour
    {
        public SurroundSlot AssignedSlot = null;
        public bool Available = false;

        public void Awake()
        {
            AssignedSlot = null;
        }

        public void AssignToSlot(SurroundSlot slot)
        {
            AssignedSlot = slot;
        }

    }
}