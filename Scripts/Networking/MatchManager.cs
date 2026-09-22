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
        [SerializeField] private string GameSceneName = "GameScene";

        [Tooltip("Scene everybody returns to when the match ends. The party is NOT left on the way -- " +
                 "going back to the lobby with the same people is the whole point.")]
        [SerializeField] private string MainMenuSceneName = "MainMenuScene";

        [Tooltip("How long the leader waits for every party member to connect before giving up on the " +
                 "stragglers and starting anyway.")]
        [SerializeField] private float ConnectWaitTimeout = 20f;

        [Tooltip("Character assigned to a client that connected without a readable payload.")]
        [SerializeField] private string DefaultCharacterId = "knight";

        [Header("Status (runtime read-out)")]
        public string Status = "Idle";

        /// <summary>Raised on the leader the moment a match start is committed to.</summary>
        public event Action MatchStarting;

        /// <summary>
        /// Server-side. Raised once a connecting client has been admitted and its identity recorded --
        /// before it has a body. Systems that keep per-player bookkeeping hook this rather than reading
        /// the roster once, because a level can come up before or after a given player arrives.
        /// </summary>
        public event Action<ulong> PlayerJoinedMatch;

        /// <summary>
        /// True once this manager has claimed the netcode callbacks it needs. Anything that starts a
        /// network on its own has to wait for it -- connecting before approval is wired up produces a
        /// player nobody has an identity for.
        /// </summary>
        public bool IsReady { get; private set; }

        private MatchJoinRequest _localRequest;

        // Server-side only, and deliberately so: it holds what each client CLAIMED, AuthId included, which
        // no other client has any business seeing. What does get replicated is the resolved loadout.
        private readonly Dictionary<ulong, MatchJoinRequest> _roster = new Dictionary<ulong, MatchJoinRequest>();

        private int _expectedPlayerCount;
        private bool _starting;
        private bool _leaving;

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
            if (!party.EveryoneReady)
            {
                SetStatus("Cannot start a match: somebody is not ready");
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
            if (NetworkManager.Singleton == null) return;

            // A pure client only ever hears about its OWN disconnect, and it means the session is over
            // for us: the host shut down, or we were dropped. Either way there is nothing left to stand
            // in, so this doubles as the "host left" path -- without it a client sits in a dead level
            // forever, which is exactly what happens today.
            if (!NetworkManager.Singleton.IsServer)
            {
                LeaveMatch().Forget();
                return;
            }

            _roster.Remove(clientId);
        }

        #endregion

        #region Leaving a match

        /// <summary>
        /// Tears this peer's match down and goes back to the lobby. Safe to call on a host or a client,
        /// and safe to call twice.
        ///
        /// Deliberately does NOT leave the party. The UGS session and the netcode connection are separate
        /// things -- the session is what holds the group together, the network is only how this match was
        /// played -- so dropping the second one leaves everybody standing in the lobby together.
        /// </summary>
        public async UniTask LeaveMatch()
        {
            if (_leaving) return;
            _leaving = true;

            try
            {
                // First, before anything else: while this is set every GameManager.ChangeScene tries to
                // be a NETWORKED load, and the network is about to stop existing. Leaving it in place
                // means the load below is handed to a dead SceneManager.
                if (GameManager.SceneLoadOverride == LoadSceneNetworked) GameManager.SceneLoadOverride = null;

                if (NetworkManager.Singleton != null) UnsubscribeFromSceneManager();

                // The host goes through the session, NOT straight to NetworkManager.Shutdown: the session
                // tracks its own NetworkState and refuses to start a second network while that still
                // reads Started, so shutting netcode down behind its back gets everyone back to the lobby
                // with a Start button that can never work again. StopNetwork stops netcode too, through
                // the session's own handler.
                //
                // A member has no such call -- the SDK keeps it internal on the client interface, because
                // only the host owns the network's lifetime -- so it just drops its own connection. That
                // is also all it needs to do: nothing on a member's side gates the next match.
                PartyManager party = PartyManager.Get();
                bool stopped = party != null && party.IsLeader && await party.StopNetwork();

                if (!stopped && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                }

                // Either path: shutdown unwinds over the next frames rather than immediately. Loading a
                // scene on top of a half-shut-down NetworkManager is how you get despawn callbacks firing
                // into a scene that no longer exists.
                float deadline = Time.realtimeSinceStartup + 5f;
                while (NetworkManager.Singleton != null
                       && NetworkManager.Singleton.ShutdownInProgress
                       && Time.realtimeSinceStartup < deadline)
                {
                    await UniTask.Yield();
                }

                _roster.Clear();
                _expectedPlayerCount = 0;

                // Locked when the match started, so it has to be unlocked or the party can never take
                // anyone new again. Host only -- a member has no say.
                if (party != null && party.IsLeader) await party.LockParty(false);

                // Each peer clears its OWN readiness, because a player property is writable only by the
                // player it belongs to -- the host cannot reset anyone else's even if it wanted to. That
                // restriction is the feature: coming back from a match, everybody has to say they are
                // ready again before the leader can drag them into another one.
                if (party != null && !party.IsLeader) await party.SetReady(false);

                SetStatus("Match ended -- back to the lobby");
                GameManager.ChangeScene(MainMenuSceneName);
            }
            finally
            {
                _leaving = false;
            }
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
            if (sceneEvent.SceneEventType != SceneEventType.Load) return;

            // Unless it is not a change at all. A client that connects to a server already standing in the
            // same scene gets synchronized into the scene it is currently in, and tearing that scene's
            // sub-managers down underneath it would break a level nobody is actually leaving.
            if (sceneEvent.SceneName == SceneManager.GetActiveScene().name) return;

            GameManager.NotifySceneLeaving();
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

        /// <summary>
        /// Gives one client a body in whatever scene is loaded right now.
        ///
        /// The normal path waits for a networked scene load to finish on every peer, which is the only
        /// correct moment when a match is travelling from the menu into a level. A level opened straight
        /// from the Editor never has such a load -- it is already the scene -- so this is how a session
        /// started in place still ends up with players in it.
        /// </summary>
        public void SpawnPlayerInCurrentScene(ulong clientId, int spawnIndex)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
            SpawnPlayerFor(clientId, spawnIndex);
        }

        private void SpawnPlayerFor(ulong clientId, int spawnIndex)
        {
            // Whoever already has a body keeps it. Both spawn paths can plausibly run over the same
            // client -- a solo session that also happens to load a scene, a reload -- and a second body
            // for one player is a far worse outcome than a skipped call.
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient connected)
                && connected.PlayerObject != null)
            {
                return;
            }

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
            PlayerJoinedMatch?.Invoke(request.ClientNetworkId);
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
