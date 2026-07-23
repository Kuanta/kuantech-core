using System;
using Kuantech.Core;

namespace Kuantech.Midcore
{
    /// <summary>
    /// One effect a trait applies to an actor, scaled by the trait's rank. A trait holds a LIST of these
    /// and runs them all, so a single talent node can combine effects (a stat bonus + a granted passive,
    /// say) without a ScriptableObject subclass per effect type — the appliers are serialized inline via
    /// [SerializeReference]. Add a new kind of trait effect by adding a TraitApplier subclass, nothing else.
    /// </summary>
    [Serializable]
    public abstract class TraitApplier
    {
        /// <summary>Applies this effect to the actor at the given (already-resolved) trait rank.</summary>
        public abstract void ApplyToActor(Actor actor, int rank);

        /// <summary>Optional effect line for the given rank, for the trait's description/UI. Empty by default.</summary>
        public virtual string GetDescription(int rank) => string.Empty;
    }
}
