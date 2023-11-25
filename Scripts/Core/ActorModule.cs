using System;
using UnityEngine;

namespace Kuantech.Core
{
    public abstract class ActorModule : MonoBehaviour {
        [NonSerialized] public Actor Actor;
        [NonSerialized] public bool Initialized;
        public abstract void Initialize();
        public abstract void Reset();
    }
}