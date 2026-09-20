using System;
using Unity.Collections;
#if NETWORKING_NGO
using Unity.Netcode;
#endif

namespace Kuantech.Networking
{
    /// <summary>
    /// What the server decided a player IS, replicated to every client. This is the verdict side of the
    /// pair whose request side is <see cref="MatchJoinRequest"/>: the server merges what the client asked
    /// for with whatever it knows about that player on its own, and publishes the result here.
    ///
    /// Right now the merge is a straight copy, because there is nothing to merge in yet. When permanent
    /// progression arrives -- traits, ranks -- it joins this type and NOT the request, and it lands here
    /// having been fetched server-side rather than claimed.
    ///
    /// Only put things in here that a client has to be able to DRAW or PREDICT. A trait that merely scales
    /// a number is applied by the server and nobody else needs to hear about it. Note also that fields
    /// must stay fixed-size for this to work as a NetworkVariable -- a variable-length inventory does not
    /// belong here, it has InventoryModule's own sync path.
    /// </summary>
    public struct PlayerLoadoutData :
#if NETWORKING_NGO
        INetworkSerializable,
#endif
        IEquatable<PlayerLoadoutData>
    {
        public FixedString64Bytes PlayerName;

        public FixedString32Bytes PlayerClass;

#if NETWORKING_NGO
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref PlayerClass);
        }
#endif

        /// <summary>
        /// Needed for the netcode to tell an actual change from a redundant write -- without it every
        /// assignment would look like a change and be sent again.
        /// </summary>
        public bool Equals(PlayerLoadoutData other)
        {
            return PlayerName.Equals(other.PlayerName) && PlayerClass.Equals(other.PlayerClass);
        }

        public override bool Equals(object obj) => obj is PlayerLoadoutData other && Equals(other);

        public override int GetHashCode() => PlayerName.GetHashCode() ^ PlayerClass.GetHashCode();

        public bool IsEmpty => PlayerName.IsEmpty;

        public override string ToString() => $"{PlayerName} ({PlayerClass})";
    }
}
