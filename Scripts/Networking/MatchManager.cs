using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
#if NETWORKING_NGO
using Unity.Netcode;
#endif

namespace Kuantech.Networking
{
    /// <summary>
    /// Owns a match's SESSION lifecycle only -- locking the party, bringing the network up, waiting for
    /// everyone to arrive, driving the scene change, and tearing back down to the menu. It used to also own
    /// connection approval, the player roster and spawning, but that was a straight duplicate of
    /// PlayerConnectionManager (see that class): whichever of the two happened to initialize first won
    /// NetworkManager.ConnectionApprovalCallback, and the loser's roster silently stayed empty forever --
    /// including, confusingly, for a real lobby-driven match whenever PlayerConnectionManager won the race.
    /// PlayerConnectionManager is now the ONLY thing that ever claims that callback, in every scene, lobby-
    /// driven or a direct-connect test scene alike (see DirectPlayBootstrap) -- SetLocalPlayerInfo below is
    /// a thin pass-through to it for exactly that reason.
    ///
    /// It has to be a PERSISTENT sub-manager. The party is captured in the menu and the match consumed in
    /// the arena, so anything scene-scoped would be destroyed in between -- which is exactly why this is not
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

        [Header("Status (runtime read-out)")]
        public string Status = "Idle";

        /// <summary>Raised on the leader the moment a match start is committed to.</summary>
        public event Action MatchStarting;

        private int _expectedPlayerCount;
        private bool _starting;
        private bool _leaving;

        public static MatchManager Get() => GetContext<MatchManager>();

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
#if NETWORKING_NGO
            if (NetworkManager.Singleton == null)
            {
                Debug.LogWarning("[MatchManager] No NetworkManager.Singleton -- matches cannot be started.");
                return;
            }

            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
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
        /// Thin pass-through to PlayerConnectionManager -- see the class doc for why identity/approval no
        /// longer lives here. Kept on this class purely so MainMenuController/PartyDebugHUD (the lobby UI
        /// this manager still serves) don't have to know that split happened.
        /// </summary>
        public void SetLocalPlayerInfo(string playerName, string characterId) =>
            PlayerConnectionManager.Get()?.SetLocalPlayerInfo(playerName, characterId);

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

            // Spawning is PlayerConnectionManager's job (it subscribes OnLoadEventCompleted itself) -- this
            // only needs the scene-leave notification, which PlayerConnectionManager doesn't cover.
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
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
        }

        private void OnClientDisconnect(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return;

            // A pure client only ever hears about its OWN disconnect, and it means the session is over
            // for us: the host shut down, or we were dropped. Either way there is nothing left to stand
            // in, so this doubles as the "host left" path -- without it a client sits in a dead level
            // forever, which is exactly what happens today. Server-side roster cleanup is
            // PlayerConnectionManager's job now, not this class's.
            if (!NetworkManager.Singleton.IsServer)
            {
                LeaveMatch().Forget();
            }
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
