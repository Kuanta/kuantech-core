using Cysharp.Threading.Tasks;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Networking
{
#if NETWORKING_NGO
    using Unity.Netcode;
#endif
    public class KtNetworkManager : SubManager
    {
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
#if NETWORKING_NGO
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
#endif
        }

        public override void Cleanup()
        {
            base.Cleanup();
#if NETWORKING_NGO
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
#endif
        }

#if NETWORKING_NGO
        /// <summary>
        /// NGO has no per-object "new observer" callback like FishNet's OnSpawnServer — every already-spawned
        /// actor stays silent by default when a new client connects. Push each one's full state explicitly,
        /// server-side only.
        /// </summary>
        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            foreach (Actor actor in ActorManager.GetAllActors())
                actor.PushStateTo(clientId);
        }
#endif

        #region Checks

        public static bool HasAuthority()
        {
#if NETWORKING_NGO
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
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
#if NETWORKING_NGO
            return NetworkManager.Singleton != null &&
                   (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient);
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
#if NETWORKING_NGO
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;
#else
            return true; // single-player: local player is always "the client"
#endif
        }

        #endregion
    }
}
