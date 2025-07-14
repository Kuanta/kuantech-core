using System;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    [Serializable]
    public class StatusEffectType
    {
        [SerializeField] private string className;
        public Type Type => string.IsNullOrEmpty(className) ? null : Type.GetType(className);
        public string ClassName => className;
    }
}