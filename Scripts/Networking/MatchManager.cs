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
    /// Owns a match from "the leader pressed start" to "everybody is standing in the arena": it locks the
    /// party, brings the network up, waits for the party to arrive, drives the scene change, and spawns a
    /// player for each client once the level exists everywhere.
    ///
    /// It has to be a PERSISTENT sub-manager. The roster is captured in the menu and consumed in the
    /// arena, so anything scene-scoped would be destroyed in between -- which is exactly why this is not
    /// part of the level's own run manager.
    /// </summary>
    public class MatchManager : SubManager
    {
        [Header("Match")]
        [Tooltip("Scene the leader loads everybody into. Must be in Build Settings.")]
        [SerializeField] private string GameSceneName = "TestScene";

        [Tooltip("How long the leader waits for every party member to connect before giving up on the " +
                 "stragglers and starting anyway.")]
        [SerializeField] private float ConnectWaitTimeout = 20f;

        [Tooltip("Character assigned to a client that connected without a readable payload.")]
        [SerializeField] private string DefaultCharacterId = "knight";

        [Header("Status (runtime read-out)")]
        public string Status = "Idle";

        /// <summary>Raised on the leader the moment a match start is committed to.</summary>
        public event Action MatchStarting;

        private MatchJoinRequest _localRequest;

        // Server-side only, and deliberately so: it holds what each client CLAIMED, AuthId included, which
        // no other client has any business seeing. What does get replicated is the resolved loadout.
        private readonly Dictionary<ulong, MatchJoinRequest> _roster = new Dictionary<ulong, MatchJoinRequest>();

        private int _expectedPlayerCount;
        private bool _starting;

        public static MatchManager Get() => GetContext<MatchManager>();

        public IReadOnlyDictionary<ulong, MatchJoinRequest> Roster => _roster;

        public bool TryGetJoinRequest(ulong clientId, out MatchJoinRequest request)
        {
            return _roster.TryGetValue(clientId, out request);
        }

        /// <summary>
        /// The merge point: what the client asked for, plus whatever the server knows about that player on
        /// its own, resolved into the one thing that actually gets replicated.
        ///
        /// Today it is a straight copy, because there is nothing server-side to merge in yet. When
        /// permanent progression exists -- traits, ranks, unlocks -- this is where the server looks it up
        /// against the backend and folds it in, and it is the only method that has to change for that.
        /// </summary>
        public PlayerLoadoutData GetPlayerLoadoutData(MatchJoinRequest request)
        {
            return new PlayerLoadoutData
            {
                PlayerName = request.PlayerName,
                PlayerClass = request.SelectedCharacterId
            };
        }

        public PlayerLoadoutData GetPlayerLoadoutData(ulong clientId)
        {
            return TryGetJoinRequest(clientId, out MatchJoinRequest request)
                ? GetPlayerLoadoutData(request)
                : default;
        }

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
#if NETWORKING_NGO
            if (NetworkManager.Singleton == null)
            {
                Debug.LogWarning("[MatchManager] No NetworkManager.Singleton -- matches cannot be started.");
                return;
            }

            // Approval is what turns the connection handshake into a place we can read the joining
            // player's claims, before anything of theirs exists in the world. The host goes through it
            // too, with its own ConnectionData, so it lands in the roster like everyone else.
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            // Netcode throws when a second callback is registered over a live one, so claim the slot only
            // when it is free -- anything already holding it was put there deliberately.
            if (NetworkManager.Singleton.ConnectionApprovalCallback == null)
            {
                NetworkManager.Singleton.ConnectionApprovalCallback = OnConnectionApproval;
            }
            else
            {
                Debug.LogWarning("[MatchManager] Something else already owns ConnectionApprovalCallback -- " +
                                 "players will connect without an identity from this manager.");
            }

            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            ApplyConnectionPayload();
#endif
        }

        public override void Cleanup()
        {
            base.Cleanup();
#if NETWORKING_NGO
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
                NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
                UnsubscribeFromSceneManager();
            }

            if (GameManager.SceneLoadOverride == LoadSceneNetworked) GameManager.SceneLoadOverride = null;
#endif
        }

        #region Local player identity

        /// <summary>
        /// Records what this player is claiming and stages it on the connection payload. Call it whenever
        /// the name or character selection changes, NOT at connect time: a member never decides when it
        /// connects -- the service connects it as soon as the leader starts the network -- so the payload
        /// has to be standing by well before that moment.
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

        // The implicit string conversion throws on overflow rather than truncating, and a name typed into
        // a text field is precisely where that would happen. Cut it here instead, well inside the limit --
        // a FixedString64Bytes holds 61 UTF-8 bytes, and non-ASCII characters cost more than one each.
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

            // Serialized with the netcode's own writer rather than as JSON, so one type covers both the
            // payload and the NetworkVariable -- JsonUtility cannot make sense of a FixedString.
            FastBufferWriter writer = new FastBufferWriter(128, Allocator.Temp, 1024);
            try
            {
                writer.WriteNetworkSerializable(_localRequest);
                NetworkManager.Singleton.NetworkConfig.ConnectionData = writer.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MatchManager] Could not write the connection payload: {e}");
            }
            finally
            {
                writer.Dispose();
            }
#endif
        }

        #endregion

#if NETWORKING_NGO

        #region Starting a match

        /// <summary>
        /// Leader only. Returns once everybody has been sent to the game scene, or false if the match
        /// could not be started at all.
        /// </summary>
        public async UniTask<bool> StartMatch()
        {
            if (_starting) return false;

            PartyManager party = PartyManager.Get();
            if (party == null || !party.InParty)
            {
                SetStatus("Cannot start a match: not in a party");
                return false;
            }
            if (!party.IsLeader)
            {
                SetStatus("Cannot start a match: only the party leader can");
                return false;
            }

            _starting = true;
            try
            {
                // Locked first, so nobody can slip in between the headcount below and everyone connecting.
                await party.LockParty(true);
                _expectedPlayerCount = Mathf.Max(1, party.PlayerCount);

                SetStatus($"Starting match for {_expectedPlayerCount} player(s)...");
                MatchStarting?.Invoke();

                if (!await party.StartNetwork())
                {
                    SetStatus("Could not start the network -- party unlocked again");
                    await party.LockParty(false);
                    return false;
                }

                await WaitForEveryone();

                SetStatus($"Loading {GameSceneName}...");
                GameManager.ChangeScene(GameSceneName);
                return true;
            }
            finally
            {
                _starting = false;
            }
        }

        /// <summary>
        /// The stragglers are not worth hanging the whole party on: past the timeout the match starts with
        /// whoever made it, rather than leaving everyone staring at a menu because one client is wedged.
        /// </summary>
        private async UniTask WaitForEveryone()
        {
            float deadline = Time.realtimeSinceStartup + ConnectWaitTimeout;

            while (NetworkManager.Singleton.ConnectedClientsIds.Count < _expectedPlayerCount)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    SetStatus($"Only {NetworkManager.Singleton.ConnectedClientsIds.Count}/" +
                              $"{_expectedPlayerCount} connected before the timeout -- starting anyway");
                    return;
                }

                SetStatus($"Waiting for players ({NetworkManager.Singleton.ConnectedClientsIds.Count}/" +
                          $"{_expectedPlayerCount})...");
                await UniTask.Yield();
            }

            SetStatus($"All {_expectedPlayerCount} player(s) connected");
        }

        #endregion

        #region Netcode lifecycle

        private void OnServerStarted()
        {
            LogNetworkConfigFingerprint("server");

            // From here on, any GameManager.ChangeScene anywhere in the game becomes a networked load.
            GameManager.SceneLoadOverride = LoadSceneNetworked;

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
        }

        private void OnClientStarted()
        {
            if (!NetworkManager.Singleton.IsServer) LogNetworkConfigFingerprint("client");

            // A pure client never loads a scene itself, but it still needs to hear that one is coming so
            // the scene it is leaving gets torn down properly.
            if (NetworkManager.Singleton.IsServer) return;
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
        }

        /// <summary>
        /// Prints exactly the fields netcode folds into the config hash it compares at connection time.
        /// When a peer is rejected with "NetworkConfig mismatch", the server's line and the client's line
        /// differ in one place, and that place is the bug -- otherwise this is guesswork, because the hash
        /// itself tells you nothing about which input produced it.
        /// </summary>
        private void LogNetworkConfigFingerprint(string role)
        {
            NetworkConfig config = NetworkManager.Singleton.NetworkConfig;

            // Sorted, because the hash sorts them too -- an unordered dictionary walk would read as a
            // difference between two peers that actually agree.
            List<uint> prefabIds = new List<uint>(config.Prefabs.NetworkPrefabOverrideLinks.Keys);
            prefabIds.Sort();

            ulong prefabFingerprint = 14695981039346656037UL;
            foreach (uint prefabId in prefabIds)
            {
                prefabFingerprint = (prefabFingerprint ^ prefabId) * 1099511628211UL;
            }

            Debug.Log($"[MatchManager] NetworkConfig fingerprint ({role}): " +
                      $"protocol={config.ProtocolVersion} tick={config.TickRate} " +
                      $"approval={config.ConnectionApproval} forceSamePrefabs={config.ForceSamePrefabs} " +
                      $"sceneManagement={config.EnableSceneManagement} " +
                      $"varLengthSafety={config.EnsureNetworkVariableLengthSafety} " +
                      $"rpcHashSize={config.RpcHashSize} " +
                      $"prefabCount={prefabIds.Count} prefabHash={prefabFingerprint:x16}");
        }

        private void UnsubscribeFromSceneManager()
        {
            if (NetworkManager.Singleton.SceneManager == null) return;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
        }

        private void OnClientDisconnect(ulong clientId)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
            _roster.Remove(clientId);
        }

        private bool LoadSceneNetworked(string sceneName)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return false;

            SceneEventProgressStatus status =
                NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status == SceneEventProgressStatus.Started) return true;

            // Refusing to claim the load lets GameManager fall back to a plain local one: single-player
            // still works, and a networked failure is loud instead of a black screen.
            Debug.LogError($"[MatchManager] Networked load of '{sceneName}' was refused ({status}) -- " +
                           "falling back to a local scene load.");
            return false;
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            // Fired locally, on every peer, just before its own load begins -- the one moment where the
            // outgoing scene is still intact and can be torn down in order.
            if (sceneEvent.SceneEventType == SceneEventType.Load) GameManager.NotifySceneLeaving();
        }

        private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            if (sceneName != GameSceneName) return;

            // Deliberately here and not on connection: a player spawned any earlier would land in the
            // menu, or in an arena that only exists on some peers.
            int spawnIndex = 0;
            foreach (ulong clientId in clientsCompleted)
            {
                SpawnPlayerFor(clientId, spawnIndex++);
            }

            if (clientsTimedOut != null && clientsTimedOut.Count > 0)
            {
                Debug.LogWarning($"[MatchManager] {clientsTimedOut.Count} client(s) timed out loading " +
                                 $"'{sceneName}' and have no player object.");
            }

            SetStatus($"Match running with {spawnIndex} player(s)");
        }

        private void SpawnPlayerFor(ulong clientId, int spawnIndex)
        {
            GameObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
            if (playerPrefab == null)
            {
                Debug.LogError("[MatchManager] NetworkConfig.PlayerPrefab is not assigned -- nobody can spawn.");
                return;
            }
            if (playerPrefab.GetComponent<NetworkObject>() == null)
            {
                Debug.LogError("[MatchManager] The player prefab has no NetworkObject -- cannot spawn it.");
                return;
            }

            PlayerSpawnPoints.GetSpawn(spawnIndex, out Vector3 position, out Quaternion rotation);

            NetworkObject player = Instantiate(playerPrefab, position, rotation).GetComponent<NetworkObject>();
            player.SpawnAsPlayerObject(clientId);

            // After the spawn, not before: a value written beforehand reaches observers as part of the
            // spawn payload with no change callback, which is a quieter path to depend on than it looks.
            // PlayerIdentityModule handles both cases anyway, but this way there is always a callback.
            PlayerIdentityModule identity = player.GetComponentInChildren<PlayerIdentityModule>();
            if (identity != null)
            {
                identity.ApplyLoadout(GetPlayerLoadoutData(clientId));
            }
            else
            {
                Debug.LogWarning("[MatchManager] The player prefab has no PlayerIdentityModule -- " +
                                 "nobody will learn this player's name.");
            }

            Debug.Log($"[MatchManager] Spawned player for clientId={clientId} " +
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
                Debug.LogWarning($"[MatchManager] clientId={request.ClientNetworkId} sent no readable " +
                                 "identity payload -- falling back to defaults.");
            }

            _roster[request.ClientNetworkId] = joinRequest;

            response.Approved = true;
            // Players are created by hand once the arena finished loading everywhere. Letting the netcode
            // create one here would drop it into whatever scene the connection happened in -- the menu.
            response.CreatePlayerObject = false;
            response.Pending = false;

            Debug.Log($"[MatchManager] Approved clientId={request.ClientNetworkId}: {joinRequest}");
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
                Debug.LogWarning($"[MatchManager] Could not read a connection payload: {e.Message}");
                return false;
            }
            finally
            {
                reader.Dispose();
            }
        }

        #endregion

#else

        public UniTask<bool> StartMatch()
        {
            SetStatus("Cannot start a match: the NETWORKING_NGO scripting define is missing");
            return UniTask.FromResult(false);
        }

#endif

        private void SetStatus(string status)
        {
            Status = status;
            Debug.Log($"[MatchManager] {status}");
        }
    }
}
