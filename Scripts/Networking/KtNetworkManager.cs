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
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                Debug.Log("[KtNetworkManager] Initialize: subscribed to OnClientConnectedCallback.");
            }
            else
            {
                Debug.LogWarning("[KtNetworkManager] Initialize: NetworkManager.Singleton is null -- OnClientConnectedCallback subscription skipped, late-join state sync will never fire.");
            }
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
            Debug.Log($"[KtNetworkManager] OnClientConnected: clientId={clientId}, IsServer={NetworkManager.Singleton.IsServer}, actorCount={ActorManager.GetAllActors().Count}.");
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

        #region Actor Lookup

        /// <summary>
        /// Resolves the Actor owned by a connected client via its auto-spawned PlayerObject
        /// (NetworkConfig.PlayerPrefab) -- the native NGO way, O(1) instead of scanning every
        /// registered Actor for a matching OwnerClientId. Any server-side system that only has a
        /// clientId (an incoming named message, an RPC param, ...) should resolve through here
        /// rather than rolling its own ActorManager.GetAllActors() search.
        /// </summary>
        public static Actor GetPlayerActor(ulong clientId)
        {
#if NETWORKING_NGO
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client)) return null;
            return client.PlayerObject != null ? client.PlayerObject.GetComponent<Actor>() : null;
#else
            return null; // offline: no such thing as "a" remote client to resolve
#endif
        }

        #endregion
    }
}
