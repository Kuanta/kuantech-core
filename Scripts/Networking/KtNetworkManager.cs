

using Cysharp.Threading.Tasks;
using Kuantech.Core;
using UnityEngine;

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
        #if NETWORKING_FISHNET
        [SerializeField] private NetworkManager NetworkManager;
        #endif
        
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

#if NETWORKING_FISHNET
        #region Client Events
        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
        {
            
        }
        #endregion
#endif

        #region Checks

        public static bool HasAuthority()
        {
#if NETWORKING_FISHNET
            var ctx = GetContext<KtNetworkManager>();
            if (ctx == null) return false;
            return ctx.NetworkManager.IsServerStarted;
#else
            return true; //Single player
#endif
        }

        /// <summary>
        /// True when any networking is active (server or client started).
        /// False in single-player / offline builds.
        /// </summary>
        public static bool IsNetworked()
        {
#if NETWORKING_FISHNET
            var ctx = GetContext<KtNetworkManager>();
            if (ctx == null) return false;
            return ctx.NetworkManager.IsServerStarted || ctx.NetworkManager.IsClientStarted;
#else
            return false;
#endif
        }

        /// <summary>
        /// True when a client connection is active. Also true on listen-server (host).
        /// False on dedicated server and in single-player.
        /// </summary>
        public static bool IsClient()
        {
#if NETWORKING_FISHNET
            var ctx = GetContext<KtNetworkManager>();
            if (ctx == null) return false;
            return ctx.NetworkManager.IsClientStarted;
#else
            return true; // single-player: local player is always "the client"
#endif
        }

        #endregion
    }
}