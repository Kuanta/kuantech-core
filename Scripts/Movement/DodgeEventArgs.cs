using System;
using UnityEngine;

namespace Kuantech.Core
{
    public class DodgeEventArgs : EventArgs
    {
        public Vector3 Direction;
        public float Duration;
    }
}
