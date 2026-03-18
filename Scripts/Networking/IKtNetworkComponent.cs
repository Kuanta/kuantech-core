#if NETWORKING_FISHNET
using FishNet.Connection;
#endif

namespace Kuantech.Networking
{
    public interface IKtNetworkComponent
    {
        void OnStartNetwork(KtActorNetworkBehaviour parentBehaviour);
        void OnStopNetwork();

        void OnStartServer();
        void OnStopServer();

        void OnStartClient();
        void OnStopClient();

        void OnStartLocalPlayer();
        void OnStopLocalPlayer();

#if NETWORKING_FISHNET
        void OnOwnershipChanged(NetworkConnection prevOwner, NetworkConnection newOwner);
#endif
    }
}