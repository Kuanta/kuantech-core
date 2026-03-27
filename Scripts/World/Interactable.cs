using Kuantech.Core;
using UnityEngine;

namespace Kuantech.World
{
    public abstract class Interactable : MonoBehaviour, IZoneElement
    {
        public Zone Zone { get; private set; }

        public virtual void Initialize(Zone zone)
        {
            Zone = zone;
        }

        public virtual void OnZoneActivated() { }
        public virtual void OnZoneDeactivated() { }

        public abstract void Interact(Actor interactor);

        public virtual void Highlight() { }
        public virtual void StopHighlight() { }
    }
}
