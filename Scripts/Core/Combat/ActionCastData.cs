using System;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]

    public class ActionCastData
    {
        public Actor Caster;
        public Vector3 StartPosition; //
        public Vector3 Direction;
        public Vector3 TargetPosition;
        public Actor Target;

        public Vector3 GetCastPoint()
        {
            if (Target != null) return Target.transform.position;
            return TargetPosition;
        }
    }
}