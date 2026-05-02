#if !NETWORKING_FISHNET
// Stub implementations of FishNet attributes and types.
// These allow modules to use [ServerRpc], [SyncVar] etc. in code that compiles
// in both offline and networked builds. In offline mode, these are no-ops.

// ReSharper disable All
using UnityEngine;

namespace FishNet.Connection
{
    public class NetworkConnection { }
}

namespace FishNet.Object
{
    public class ServerRpcAttribute : System.Attribute
    {
        public bool RequireOwnership = true;
        public bool RunLocally = false;
    }

    public class ObserversRpcAttribute : System.Attribute
    {
        public bool BufferLast = false;
        public bool RunLocally = false;
    }

    public class TargetRpcAttribute : System.Attribute
    {
        public bool RunLocally = false;
    }

    public class ClientRpcAttribute : System.Attribute
    {
        public bool RunLocally = false;
    }
}

namespace FishNet.Object.Synchronizing
{
    // Stub for FishNet v4 SyncVar<T> — no-op in offline builds.
    // In online builds, FishNet's IL Weaver handles replication automatically.
    public class SyncVar<T>
    {
        public T Value { get; set; }
        public event System.Action<T, T, bool> OnChange;
    }
}

namespace Kuantech.Networking{
    public abstract class KtNetworkBehaviourStub : MonoBehaviour
    {
        public virtual void OnStartNetwork(){}

        public virtual void OnStopNetwork(){}

        public virtual void OnStartClient(){}

        public virtual void OnStopClient(){}

        public virtual void OnOwnershipClient(FishNet.Connection.NetworkConnection prevOwner){}
    }
}
#endif
