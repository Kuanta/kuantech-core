using Unity.Collections;
#if NETWORKING_NGO
using Unity.Netcode;
#endif

namespace Kuantech.Networking
{
    /// <summary>
    /// What a client DECLARES about itself on the way in, carried in the connection payload. Everything
    /// here is the player's own claim -- the server has no independent way to know any of it, which is
    /// exactly why nothing earned belongs in this type. Ranks, traits, unlocks and the like are looked up
    /// server-side against the backend; a client that could send them would send whatever it liked.
    ///
    /// The server turns this into a <see cref="PlayerLoadoutData"/> (see MatchManager.GetPlayerLoadoutData)
    /// and only that result is ever replicated. Keeping the two apart is the whole point: this is a
    /// request, that is the verdict.
    /// </summary>
    public struct MatchJoinRequest
#if NETWORKING_NGO
        : INetworkSerializable
#endif
    {
        /// <summary>
        /// The UGS player id this client claims to be. Server-side only -- it never reaches the replicated
        /// loadout, because no other client has any use for it. Note that it is a claim like everything
        /// else here: trusting it enough to look up somebody's progression means first checking it against
        /// the party session's own player list, which comes from the service rather than from a client.
        /// </summary>
        public FixedString64Bytes AuthId;

        public FixedString64Bytes PlayerName;

        public FixedString32Bytes SelectedCharacterId;

#if NETWORKING_NGO
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref AuthId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref SelectedCharacterId);
        }
#endif

        public override string ToString()
        {
            return $"{PlayerName} (character '{SelectedCharacterId}', auth '{AuthId}')";
        }
    }
}
