using Kuantech.Core;

namespace Kuantech.World
{
    public interface IInteractable
    {
        void Interact(Actor interactor);
        public virtual void Highlight() { }
        public virtual void StopHighlight() { }

        /// <summary>
        /// Whether this should be offered to the interactor at all right now -- highlighted, prompted,
        /// reachable. Distinct from whether the interaction would succeed: a wall-buy you cannot afford
        /// still says so, which is more useful than going invisible.
        ///
        /// Defaults to true, so anything that was always interactable stays that way.
        /// </summary>
        public virtual bool IsInteractable(Actor interactor) => true;

        /// <summary>
        /// Short label for the on-screen prompt -- "Revive", "Buy MP5 (1000)", "Open". The key to press
        /// is not part of this: that is a binding, and it belongs wherever the prompt is drawn rather than
        /// repeated in every interactable.
        ///
        /// Defaults to empty, which reads as "no prompt" -- existing interactables show nothing until
        /// somebody gives them words.
        /// </summary>
        public virtual string GetInteractionText(Actor interactor) => string.Empty;
    }
}
