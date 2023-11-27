using System;
using UnityEngine;

namespace Kuantech.Core
{
    public abstract class ActorModule : MonoBehaviour {
        [NonSerialized] public Actor Actor;
        [NonSerialized] public bool Initialized;
        public virtual void Initialize(){}
        public virtual void OnModulesInitialized(){}
        public virtual void Reset(){}
    }
}