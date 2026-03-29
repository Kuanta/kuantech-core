using Kuantech.Core;

namespace Kuantech.World
{
    public interface IInteractable
    {
        void Interact(Actor interactor);
        public virtual void Highlight() { }
        public virtual void StopHighlight() { }
    }
}