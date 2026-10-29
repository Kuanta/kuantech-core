namespace Kuantech.Core
{
    /// <summary>
    /// What an Actor needs to know about the network, said without naming one. Core code asks these
    /// questions and never learns who answers them: offline it is <see cref="OfflineActorNetwork"/>, in a
    /// networked project a sibling component (for NGO, NetworkedActor) registers itself in its place.
    ///
    /// Two roles, because they are not the same peer. Authority decides game truth (damage, state changes);
    /// the locally controlled peer is the one whose input drives the actor. A host holds both for its own
    /// player, a remote client holds only the second, and the server holds only the first for an enemy.
    /// </summary>
    public interface IActorNetwork
    {
        /// <summary>This peer decides game truth for the actor. Always true offline.</summary>
        bool HasAuthority { get; }

        /// <summary>This peer's input drives the actor. Always true offline.</summary>
        bool IsLocallyControlled { get; }

        /// <summary>Id of the peer that controls the actor. 0 offline.</summary>
        ulong OwnerId { get; }

        /// <summary>
        /// This peer shows the game to a player. False only where nothing is ever on screen (a dedicated
        /// server), which lets presentation code skip UI and effects. Always true offline.
        /// </summary>
        bool HasPresentation { get; }

        /// <summary>
        /// Despawns the actor through the network layer when it manages the actor's lifetime. Returns false
        /// when it does not, in which case the actor despawns itself locally.
        /// </summary>
        bool TryDespawn();
    }

    /// <summary>
    /// Implemented by a component that attaches network behaviour to an Actor. Actor finds these the same way
    /// it finds modules and calls <see cref="Bind"/> once, after every module is discovered and before any
    /// module is initialized, so a binding can hook module events before the first thing fires them.
    /// </summary>
    public interface IActorNetBinding
    {
        void Bind(Actor actor);
    }

    /// <summary>The network every Actor starts with: this peer is the only one, so it is in charge of everything.</summary>
    public sealed class OfflineActorNetwork : IActorNetwork
    {
        public static readonly OfflineActorNetwork Instance = new OfflineActorNetwork();

        private OfflineActorNetwork() { }

        public bool HasAuthority => true;
        public bool IsLocallyControlled => true;
        public ulong OwnerId => 0;
        public bool HasPresentation => true;
        public bool TryDespawn() => false;
    }
}
