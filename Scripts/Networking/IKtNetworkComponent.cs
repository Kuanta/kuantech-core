#if NETWORKING_FISHNET
    using FishNet.Connection;
#endif

namespace Kuantech.Networking
{
    public interface IKtNetworkComponent
    {
#if NETWORKING_FISHNET
        public void OnStartNetwork(KtActorNetworkBehaviour parentBehavior);
        void OnStopNetwork();
        
        public void OnStartServer();
        public void OnStopServer();
        
        public void OnStartClient();
        public void OnStopClient();
        
       public void OnStartLocalPlayer();
       public void OnStopLocalPlayer();
       
       void OnOwnershipChanged(NetworkConnection prevOwner, NetworkConnection newOwner);
#endif
    }
}