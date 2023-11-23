using UnityEngine;

namespace Kuantech.Core
{
    public abstract class ActorModule : MonoBehaviour {
        public Actor Actor;
        public bool Initialized;
        public abstract void Initialize();
        public abstract void Reset();
    }
}