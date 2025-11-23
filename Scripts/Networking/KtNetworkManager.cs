

namespace Kuantech.Networking
{
#if NETWORKING_FISHNET
    using Cysharp.Threading.Tasks;
    using FishNet.Managing;
    using FishNet.Transporting;
    using Kuantech.Core;
    using UnityEngine;

    public class KtNetworkManager : SubManager
    {
        [SerializeField] private NetworkManager NetworkManager;
        
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            if (NetworkManager == null)
            {
                NetworkManager = gameManager.GetComponent<NetworkManager>();
            }
            
            //Server Manager
            
            //ClientManager
            NetworkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
        }
        
        #region Client Events
        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
        {
            
        }
        #endregion
    }
#endif
}