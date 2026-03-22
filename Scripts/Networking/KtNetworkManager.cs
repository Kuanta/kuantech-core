

namespace Kuantech.Networking
{
#if NETWORKING_FISHNET
    using Cysharp.Threading.Tasks;
    using FishNet.Managing;
    using FishNet.Transporting;
    using Kuantech.Core;
    using UnityEngine;
#endif
    public class KtNetworkManager : SubManager
    {
        [SerializeField] private NetworkManager NetworkManager;
        
        public override async UniTask Initialize(GameManager gameManager)
        {
#if NETWORKING_FISHNET
            await base.Initialize(gameManager);
            if (NetworkManager == null)
            {
                NetworkManager = gameManager.GetComponent<NetworkManager>();
            }
            
            //Server Manager
            
            //ClientManager
            NetworkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
#endif
        }

        #region Client Events
        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
        {
            
        }
        #endregion

        #region Checks

        public static bool HasAuthority()
        {
#if NETWORKING_FISHNET
            var ctx = GetContext<KtNetworkManager>();
            if (ctx == null) return true; //Single player
            return ctx.NetworkManager.IsServerStarted;
#else
            return true;
#endif
        }
        #endregion
    }
}