using System;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]

    public class ActionCastData
    {
        public Actor Caster;
        public Vector3 StartPosition; //Start position of the cast
        public Vector3 Direction; //Direction of the cast
        public Vector3 TargetPosition; //Targeted position
        public Actor Target; //Targeted actor
        public bool OverrideRotation = true;

        public Vector3 GetCastPoint()
        {
            if (Target != null) return Target.transform.position;
            return TargetPosition;
        }
    }
}