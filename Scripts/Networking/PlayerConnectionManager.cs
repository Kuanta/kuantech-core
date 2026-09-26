using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if NETWORKING_NGO
using Unity.Netcode;
#endif

namespace Kuantech.Networking
{
    /// <summary>
    /// Owns NGO connections and the player bodies attached to them -- and nothing above that. It has no
    /// idea LobbyManager, a session, or any pre-connection concept exists: everything it needs about a
    /// connecting client (name, chosen character) arrives on the connection payload itself, written by
    /// whoever brought this client's network up (a direct test bootstrap today, a lobby-driven flow
    /// later). That split is the whole point -- every manager that does network work should be able to do
    /// its job without knowing how the connection got there.
    ///
    /// Persistent: connections and their bodies span scene loads (NGO preserves a spawned player's
    /// NetworkObject across a LoadSceneMode.Single load by default -- nothing here or anywhere else
    /// destroys and recreates a player on a scene change), so anything that tracks them has to survive
    /// scene loads too.
    /// </summary>
    public class PlayerConnectionManager : SubManager
    {
        [Tooltip("Character assigned to a client that connected without a readable payload.")]
        [SerializeField] private string DefaultCharacterId = "knight";

        [Tooltip("How long to wait for the level to register spawn points before giving up and placing " +
                 "a new player at the origin anyway.")]
        [SerializeField] private float SpawnPointWaitTimeout = 5f;

        [Header("Status (runtime read-out)")]
        public string Status = "Idle";

        /// <summary>
        /// True once this manager has claimed the netcode callbacks it needs. Anything that starts a
        /// network on its own has to wait for it -- connecting before approval is wired up produces a
        /// player nobody has an identity for.
        /// </summary>
        public bool IsReady { get; private set; }

        /// <summary>
        /// Fired for a client that already had a body when a scene finished loading -- the
        /// surviving-player case NGO raises no event for, since nothing actually spawned. What to do about
        /// it (reposition at this scene's spawn points, rebind a camera, ...) is scene-specific and
        /// deliberately not this manager's call; it only reports the fact.
        /// </summary>
        public event Action<Actor> ExistingPlayerEnteredScene;

        /// <summary>
        /// Server-side. Raised once a connecting client has been admitted and its identity recorded --
        /// before it has a body. Same role as MatchManager.PlayerJoinedMatch, for scenes that use this
        /// manager instead (see DirectPlayBootstrap) -- systems that keep per-player bookkeeping (run
        /// state, persistent profiles, ...) and want to work regardless of which of the two owns
        /// connection approval in a given scene should subscribe to both and let whichever one is actually
        /// live fire.
        /// </summary>
        public event Action<ulong> PlayerJoined;

        private MatchJoinRequest _localRequest;

        // Server-side only, and deliberately so: it holds what each client CLAIMED, AuthId included, which
        // no other client has any business seeing. What does get replicated is the resolved loadout.
        private readonly Dictionary<ulong, MatchJoinRequest> _roster = new Dictionary<ulong, MatchJoinRequest>();

        private int _spawnIndex;

        public static PlayerConnectionManager Get() => GetContext<PlayerConnectionManager>();

        public IReadOnlyDictionary<ulong, MatchJoinRequest> Roster => _roster;

        public PlayerLoadoutData GetPlayerLoadoutData(ulong clientId)
        {
            return _roster.TryGetValue(clientId, out MatchJoinRequest request)
                ? new PlayerLoadoutData { PlayerName = request.PlayerName, PlayerClass = request.SelectedCharacterId }
                : default;
        }

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
#if NETWORKING_NGO
            if (NetworkManager.Singleton == null)
            {
                Debug.LogWarning("[PlayerConnectionManager] No NetworkManager.Singleton -- connections cannot be handled.");
                return;
            }

            // Approval is what turns the connection handshake into a place we can read the joining
            // player's claims, before anything of theirs exists in the world. The host goes through it
            // too, with its own ConnectionData, so it lands in the roster like everyone else.
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            if (NetworkManager.Singleton.ConnectionApprovalCallback == null)
            {
                NetworkManager.Singleton.ConnectionApprovalCallback = OnConnectionApproval;
            }
            else
            {
                Debug.LogWarning("[PlayerConnectionManager] Something else already owns ConnectionApprovalCallback -- " +
                                 "players will connect without an identity from this manager.");
            }

            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            ApplyConnectionPayload();
            IsReady = true;
#endif
        }

        public override void Cleanup()
        {
            base.Cleanup();
#if NETWORKING_NGO
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
                if (NetworkManager.Singleton.SceneManager != null)
                    NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            }

            if (GameManager.SceneLoadOverride == LoadSceneNetworked) GameManager.SceneLoadOverride = null;
#endif
        }

        #region Local identity

        /// <summary>
        /// Records what this player is claiming and stages it on the connection payload. Call it before
        /// connecting -- the payload has to be standing by well before that moment, not set at connect time.
        /// </summary>
        public void SetLocalPlayerInfo(string playerName, string characterId)
        {
            UgsManager ugs = UgsManager.Get();

            _localRequest = new MatchJoinRequest
            {
                AuthId = ToFixed64(ugs != null ? ugs.PlayerId : ""),
                PlayerName = ToFixed64(playerName),
                SelectedCharacterId = ToFixed32(characterId)
            };

            ApplyConnectionPayload();
        }

        private static FixedString64Bytes ToFixed64(string value)
        {
            if (string.IsNullOrEmpty(value)) return default;
            return value.Length <= 20 ? value : value.Substring(0, 20);
        }

        private static FixedString32Bytes ToFixed32(string value)
        {
            if (string.IsNullOrEmpty(value)) return default;
            return value.Length <= 12 ? value : value.Substring(0, 12);
        }

        private void ApplyConnectionPayload()
        {
#if NETWORKING_NGO
            if (NetworkManager.Singleton == null) return;

            FastBufferWriter writer = new FastBufferWriter(128, Allocator.Temp, 1024);
            try
            {
                writer.WriteNetworkSerializable(_localRequest);
                NetworkManager.Singleton.NetworkConfig.ConnectionData = writer.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerConnectionManager] Could not write the connection payload: {e}");
            }
            finally
            {
                writer.Dispose();
            }
#endif
        }

        #endregion

#if NETWORKING_NGO

        #region Netcode lifecycle

        private void OnServerStarted()
        {
            // From here on, any GameManager.ChangeScene anywhere in the game becomes a networked load.
            GameManager.SceneLoadOverride = LoadSceneNetworked;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }

        private bool LoadSceneNetworked(string sceneName)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return false;

            SceneEventProgressStatus status =
                NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status == SceneEventProgressStatus.Started) return true;

            // Refusing to claim the load lets GameManager fall back to a plain local one: single-player
            // still works, and a networked failure is loud instead of a black screen.
            Debug.LogError($"[PlayerConnectionManager] Networked load of '{sceneName}' was refused ({status}) -- " +
                           "falling back to a local scene load.");
            return false;
        }

        private void OnClientDisconnect(ulong clientId)
        {
            _roster.Remove(clientId);
        }

        #endregion

        #region Spawning

        /// <summary>
        /// Covers the path a networked scene load never fires for: a bootstrap that starts a host in a
        /// scene already sitting there (no NetworkSceneManager load happened, so OnLoadEventCompleted
        /// never runs). OnClientConnectedCallback fires regardless, for every connection including the
        /// host's own.
        /// </summary>
        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            SpawnWhenLevelIsReady(clientId, _spawnIndex++).Forget();
        }

        /// <summary>
        /// The host's own connection callback fires from inside StartHost, earlier than the level's own
        /// reaction to that same start -- so on the first frame there is genuinely nowhere to stand yet.
        /// Waiting for the level to register its spawn points is the honest dependency; the timeout is
        /// there so a level with none still gets a player, at the origin, with the warning that comes
        /// with it.
        /// </summary>
        private async UniTaskVoid SpawnWhenLevelIsReady(ulong clientId, int spawnIndex)
        {
            float deadline = Time.realtimeSinceStartup + SpawnPointWaitTimeout;
            while (!PlayerSpawnPoints.HasAny && Time.realtimeSinceStartup < deadline)
            {
                await UniTask.Yield();
            }

            // The session can end while we wait -- leaving play mode, a shutdown mid-load.
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            SpawnPlayerFor(clientId, spawnIndex);
        }

        /// <summary>
        /// Covers the other path: a real networked scene load (GameManager.ChangeScene while networked)
        /// finishing on every peer. Whoever already has a body from the scene they just left keeps it --
        /// see SpawnPlayerFor -- this is where that client's existence gets reported for this new scene.
        /// </summary>
        private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            foreach (ulong clientId in clientsCompleted)
            {
                SpawnPlayerFor(clientId, _spawnIndex++);
            }

            if (clientsTimedOut != null && clientsTimedOut.Count > 0)
            {
                Debug.LogWarning($"[PlayerConnectionManager] {clientsTimedOut.Count} client(s) timed out loading " +
                                 $"'{sceneName}' and have no player object.");
            }
        }

        /// <summary>
        /// Gives a client a body if it doesn't already have one. A survivor from the previous scene keeps
        /// its object and is reported through ExistingPlayerEnteredScene instead -- see the class doc for
        /// why nothing here destroys and recreates it.
        /// </summary>
        private void SpawnPlayerFor(ulong clientId, int spawnIndex)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient connected)
                && connected.PlayerObject != null)
            {
                Actor existing = connected.PlayerObject.GetComponent<Actor>();
                if (existing != null) ExistingPlayerEnteredScene?.Invoke(existing);
                return;
            }

            GameObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerConnectionManager] NetworkConfig.PlayerPrefab is not assigned -- nobody can spawn.");
                return;
            }
            if (playerPrefab.GetComponent<NetworkObject>() == null)
            {
                Debug.LogError("[PlayerConnectionManager] The player prefab has no NetworkObject -- cannot spawn it.");
                return;
            }

            PlayerSpawnPoints.GetSpawn(spawnIndex, out Vector3 position, out Quaternion rotation);

            NetworkObject player = Instantiate(playerPrefab, position, rotation).GetComponent<NetworkObject>();
            player.SpawnAsPlayerObject(clientId);

            // After the spawn, not before: a value written beforehand reaches observers as part of the
            // spawn payload with no change callback, which is a quieter path to depend on than it looks.
            PlayerIdentityModule identity = player.GetComponentInChildren<PlayerIdentityModule>();
            if (identity != null)
            {
                identity.ApplyLoadout(GetPlayerLoadoutData(clientId));
            }
            else
            {
                Debug.LogWarning("[PlayerConnectionManager] The player prefab has no PlayerIdentityModule -- " +
                                 "nobody will learn this player's name.");
            }

            Debug.Log($"[PlayerConnectionManager] Spawned player for clientId={clientId} " +
                      $"({GetPlayerLoadoutData(clientId)}) at {position}.");
        }

        #endregion

        #region Connection approval

        private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            if (!TryReadPayload(request.Payload, out MatchJoinRequest joinRequest))
            {
                // Approving anyway: an unreadable payload means a stale or mismatched build, which is a
                // problem worth a log, not worth locking a friend out of the game over.
                joinRequest = new MatchJoinRequest
                {
                    AuthId = default,
                    PlayerName = ToFixed64($"Player {request.ClientNetworkId}"),
                    SelectedCharacterId = ToFixed32(DefaultCharacterId)
                };
                Debug.LogWarning($"[PlayerConnectionManager] clientId={request.ClientNetworkId} sent no readable " +
                                 "identity payload -- falling back to defaults.");
            }

            _roster[request.ClientNetworkId] = joinRequest;

            response.Approved = true;
            // Players are created by hand once the level actually has spawn points -- letting the netcode
            // create one here would drop it wherever the connection happened to occur.
            response.CreatePlayerObject = false;
            response.Pending = false;

            Debug.Log($"[PlayerConnectionManager] Approved clientId={request.ClientNetworkId}: {joinRequest}");
            PlayerJoined?.Invoke(request.ClientNetworkId);
        }

        private static bool TryReadPayload(byte[] payload, out MatchJoinRequest request)
        {
            request = default;
            if (payload == null || payload.Length == 0) return false;

            FastBufferReader reader = new FastBufferReader(payload, Allocator.Temp);
            try
            {
                reader.ReadNetworkSerializable(out request);
                return !request.PlayerName.IsEmpty;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerConnectionManager] Could not read a connection payload: {e.Message}");
                return false;
            }
            finally
            {
                reader.Dispose();
            }
        }

        #endregion

#endif
    }
}
