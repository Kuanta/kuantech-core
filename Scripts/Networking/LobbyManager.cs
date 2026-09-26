using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using UnityEngine;
#if UGS_SERVICES
using Unity.Services.Multiplayer;
#endif

namespace Kuantech.Networking
{
    /// <summary>
    /// One member of a lobby, flattened out of whatever backend <see cref="LobbyManager"/> talks to.
    /// UI and gameplay code only ever see this -- nothing outside LobbyManager references the session
    /// SDK's own types, so callers need no compile guard of their own and the backend stays swappable.
    /// </summary>
    public readonly struct LobbyMember
    {
        public readonly string Id;
        public readonly string Name;
        public readonly bool IsLeader;

        public LobbyMember(string id, string name, bool isLeader)
        {
            Id = id;
            Name = name;
            IsLeader = isLeader;
        }
    }

    /// <summary>
    /// The lobby layer, and only the lobby layer: create a party, join one by its code, track who is in
    /// it, and know who leads it. Deliberately has no idea NGO or any other gameplay network even exists --
    /// bringing a network up (relay, connection payloads, spawning) is a project-specific concern that
    /// belongs above this, not inside it, precisely so this class stays reusable across projects. No
    /// ready-check either: that is a UX choice a specific game makes, not something every lobby needs.
    ///
    /// This intentionally duplicates PartyManager's session plumbing rather than replacing it -- the old
    /// MainMenuController/PartyDebugHUD screens still depend on PartyManager's ready-check surface and are
    /// left alone as-is. LobbyManager is what the Hub-based flow builds on going forward.
    /// </summary>
    public class LobbyManager : SubManager
    {
        [Header("Lobby")]
        [Tooltip("Party size cap, the leader included.")]
        public int MaxPlayers = 4;

        [Tooltip("Client-side key tagging every session this game creates. It keeps this game's parties " +
                 "distinguishable from anything else sharing the same UGS project -- it is NOT a filter " +
                 "on who may join, since joining happens by code.")]
        public string SessionType = "castle-defenders-lobby";

        [Header("Status (runtime read-out)")]
        public string Status = "Not in a lobby";
        [Tooltip("The code a friend types to join this lobby. Empty when not in one.")]
        public string JoinCode = "";

        /// <summary>Raised whenever lobby membership changed -- redraw from Members.</summary>
        public event Action LobbyChanged;

        /// <summary>Raised with a human-readable reason when a lobby operation fails.</summary>
        public event Action<string> LobbyFailed;

        private readonly List<LobbyMember> _members = new List<LobbyMember>();
        public IReadOnlyList<LobbyMember> Members => _members;

        /// <summary>True while a create/join round-trip is in flight -- disable the buttons on it.</summary>
        public bool IsBusy { get; private set; }

        public static LobbyManager Get() => GetContext<LobbyManager>();

#if UGS_SERVICES
        private ISession _session;

        public bool InLobby => _session != null;
        public bool IsLeader => _session != null && _session.IsHost;
        public int PlayerCount => _session != null ? _session.PlayerCount : 0;
#else
        public bool InLobby => false;
        public bool IsLeader => false;
        public int PlayerCount => 0;
#endif

        public override void Cleanup()
        {
            base.Cleanup();
            // Fire-and-forget: Cleanup is synchronous, and a lobby we fail to leave expires service-side
            // anyway. Leaving explicitly just drops us out of everyone else's list right away.
            LeaveLobby().Forget();
        }

        #region Lobby operations

        /// <summary>
        /// Sets the name this player shows up as for everyone else. Call it before creating or joining --
        /// the name is captured into the session at that point.
        /// </summary>
        public async UniTask<bool> SetPlayerName(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName)) return false;
#if UGS_SERVICES
            if (!await EnsureSignedIn()) return false;

            // UGS appends a discriminator of its own (#1234), so the stored name never equals the typed
            // one -- compare only the part we actually set. Worth the trouble because this runs on every
            // create/join, and UpdatePlayerNameAsync is rate limited: re-sending an unchanged name during
            // repeated testing is exactly how that limit gets hit.
            string trimmedName = playerName.Trim();
            string currentName = Unity.Services.Authentication.AuthenticationService.Instance.PlayerName;
            if (!string.IsNullOrEmpty(currentName))
            {
                int discriminatorIndex = currentName.IndexOf('#');
                string currentBaseName = discriminatorIndex >= 0
                    ? currentName.Substring(0, discriminatorIndex)
                    : currentName;
                if (currentBaseName == trimmedName) return true;
            }

            try
            {
                await Unity.Services.Authentication.AuthenticationService.Instance.UpdatePlayerNameAsync(trimmedName);
                return true;
            }
            catch (Exception e)
            {
                Fail("Set player name", e);
                return false;
            }
#else
            return FailNoDefine("Set player name");
#endif
        }

        public async UniTask<bool> CreateLobby()
        {
#if UGS_SERVICES
            if (IsBusy) return false;
            IsBusy = true;
            try
            {
                if (!await EnsureSignedIn()) return false;
                if (InLobby) await LeaveLobby();

                SetStatus("Creating lobby...");
                SessionOptions options = new SessionOptions
                {
                    MaxPlayers = MaxPlayers,
                    // Hidden from session queries and quick-join: a lobby is reachable ONLY by its code,
                    // which is the whole point -- nobody stumbles into a friends-only run.
                    IsPrivate = true,
                    Type = SessionType
                }.WithPlayerName();

                AdoptSession(await MultiplayerService.Instance.CreateSessionAsync(options));
                SetStatus($"Lobby created -- code {JoinCode}");
                return true;
            }
            catch (Exception e)
            {
                Fail("Create lobby", e);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
#else
            await UniTask.CompletedTask;
            return FailNoDefine("Create lobby");
#endif
        }

        public async UniTask<bool> JoinLobby(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                Fail("Join lobby", "no code entered");
                return false;
            }
#if UGS_SERVICES
            if (IsBusy) return false;
            IsBusy = true;
            try
            {
                if (!await EnsureSignedIn()) return false;
                if (InLobby) await LeaveLobby();

                string trimmedCode = code.Trim().ToUpperInvariant();
                SetStatus($"Joining lobby {trimmedCode}...");

                JoinSessionOptions options = new JoinSessionOptions { Type = SessionType }.WithPlayerName();
                AdoptSession(await MultiplayerService.Instance.JoinSessionByCodeAsync(trimmedCode, options));
                SetStatus($"Joined lobby {JoinCode}");
                return true;
            }
            catch (Exception e)
            {
                Fail("Join lobby", e);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
#else
            await UniTask.CompletedTask;
            return FailNoDefine("Join lobby");
#endif
        }

        /// <summary>
        /// Leader only. A locked lobby rejects further joins -- whoever ends up orchestrating "everyone's
        /// in, bring the network up" calls this first so the roster cannot grow underneath it.
        /// </summary>
        public async UniTask<bool> LockLobby(bool locked)
        {
#if UGS_SERVICES
            if (_session == null || !_session.IsHost) return false;
            try
            {
                IHostSession host = _session.AsHost();
                host.IsLocked = locked;
                await host.SavePropertiesAsync();
                return true;
            }
            catch (Exception e)
            {
                Fail("Lock lobby", e);
                return false;
            }
#else
            await UniTask.CompletedTask;
            return FailNoDefine("Lock lobby");
#endif
        }

        /// <summary>
        /// Leader only. Brings the game server up for the whole lobby over Relay. Members do not call
        /// this: the session's own network bridge starts each member's client automatically once it sees
        /// the host's side come up (via the same session-changed events this class already subscribes to)
        /// -- there is no separate "tell the lobby" step to write, the SDK's session sync IS that
        /// notification. This is the one thing about a session that unavoidably lives here even though it
        /// crosses into networking: only this class holds the ISession handle StartRelayNetworkAsync needs,
        /// and the call itself never touches an NGO type -- it asks the SESSION to start its network,
        /// nothing here has to know what NetworkManager even is.
        ///
        /// WHEN this gets called (Hub's own bootstrap today, an Entry/Transition scene later, a future
        /// Start button after that) is deliberately not this method's business -- moving that decision
        /// around later is a one-line change at whichever call site, not a structural one.
        /// </summary>
        public async UniTask<bool> StartGameServer()
        {
#if UGS_SERVICES
            if (_session == null || !_session.IsHost)
            {
                Fail("Start game server", "only the lobby leader can start the game server");
                return false;
            }
            try
            {
                SetStatus("Starting game server...");
                await _session.AsHost().Network.StartRelayNetworkAsync(RelayNetworkOptions.Default);
                SetStatus("Game server started");
                return true;
            }
            catch (Exception e)
            {
                Fail("Start game server", e);
                return false;
            }
#else
            await UniTask.CompletedTask;
            return FailNoDefine("Start game server");
#endif
        }

        /// <summary>
        /// The counterpart to StartGameServer, leader-only for the same reason: the SDK exposes
        /// StopNetworkAsync on IHostSessionNetwork but keeps it internal on IClientSessionNetwork, because
        /// only the host owns the network's lifetime. A member's side goes away on its own when the
        /// host's does.
        /// </summary>
        public async UniTask<bool> StopGameServer()
        {
#if UGS_SERVICES
            if (_session == null || !_session.IsHost) return false;
            try
            {
                await _session.AsHost().Network.StopNetworkAsync();
                SetStatus("Game server stopped");
                return true;
            }
            catch (Exception e)
            {
                // Throws when it was not Started -- a session that never brought a server up, or a second
                // call. Not worth failing over, so this is a note, not a Fail.
                Debug.LogWarning($"[LobbyManager] Stop game server: {e.Message}");
                return false;
            }
#else
            await UniTask.CompletedTask;
            return false;
#endif
        }

        public async UniTask LeaveLobby()
        {
#if UGS_SERVICES
            if (_session == null) return;

            ISession leaving = _session;
            ClearSession(); // dropped locally first, so the UI reacts even if the call below hangs
            try
            {
                await leaving.LeaveAsync();
                SetStatus("Left the lobby");
            }
            catch (Exception e)
            {
                Fail("Leave lobby", e);
            }
#else
            await UniTask.CompletedTask;
#endif
        }

        #endregion

#if UGS_SERVICES

        #region Session plumbing

        private void AdoptSession(ISession session)
        {
            _session = session;
            JoinCode = session.Code;

            session.Changed += OnSessionChanged;
            session.PlayerJoined += OnPlayerJoined;
            session.PlayerHasLeft += OnPlayerHasLeft;
            session.Deleted += OnSessionDeleted;
            session.RemovedFromSession += OnRemovedFromSession;

            RebuildMembers();
            LobbyChanged?.Invoke();
        }

        private void ClearSession()
        {
            if (_session != null)
            {
                _session.Changed -= OnSessionChanged;
                _session.PlayerJoined -= OnPlayerJoined;
                _session.PlayerHasLeft -= OnPlayerHasLeft;
                _session.Deleted -= OnSessionDeleted;
                _session.RemovedFromSession -= OnRemovedFromSession;
                _session = null;
            }

            JoinCode = "";
            _members.Clear();
            LobbyChanged?.Invoke();
        }

        /// <summary>
        /// Rebuilt wholesale on every change rather than patched member-by-member: a lobby is a handful
        /// of entries, and the service can hand us a state we never saw the individual deltas for (a
        /// rejoin, a refresh, a batch of updates that arrived as one notification).
        /// </summary>
        private void RebuildMembers()
        {
            _members.Clear();
            if (_session == null) return;

            foreach (IReadOnlyPlayer player in _session.Players)
            {
                string playerName = player.GetPlayerName();
                _members.Add(new LobbyMember(
                    player.Id,
                    string.IsNullOrEmpty(playerName) ? player.Id : playerName,
                    player.Id == _session.Host));
            }
        }

        private void OnSessionChanged()
        {
            RebuildMembers();
            LobbyChanged?.Invoke();
        }

        private void OnPlayerJoined(string playerId) => OnSessionChanged();

        private void OnPlayerHasLeft(string playerId) => OnSessionChanged();

        private void OnSessionDeleted()
        {
            SetStatus("The leader closed the lobby");
            ClearSession();
        }

        private void OnRemovedFromSession()
        {
            SetStatus("Removed from the lobby");
            ClearSession();
        }

        private async UniTask<bool> EnsureSignedIn()
        {
            UgsManager ugs = UgsManager.Get();
            if (ugs == null)
            {
                Fail("Sign in", "no UgsManager sub-manager is registered");
                return false;
            }
            if (await ugs.EnsureSignedIn()) return true;

            Fail("Sign in", "UgsManager could not sign in -- check its Status field");
            return false;
        }

        #endregion

#endif

        #region Status

        private void SetStatus(string status)
        {
            Status = status;
            Debug.Log($"[LobbyManager] {status}");
        }

        private void Fail(string operation, Exception e)
        {
#if UGS_SERVICES
            // SessionException carries a specific SessionError (session full, invalid code, not
            // authorized, ...) that the bare message tends to bury -- surface it, it is the first thing
            // worth knowing when a join does not work.
            string reason = e is SessionException sessionException
                ? $"{sessionException.Error} -- {sessionException.Message}"
                : e.Message;
#else
            string reason = e.Message;
#endif
            Fail(operation, reason);
            Debug.LogError($"[LobbyManager] {operation} failed: {e}");
        }

        private void Fail(string operation, string reason)
        {
            Status = $"{operation} failed: {reason}";
            LobbyFailed?.Invoke(Status);
        }

        private bool FailNoDefine(string operation)
        {
            Fail(operation, "the UGS_SERVICES scripting define is missing, so the lobby layer is compiled out");
            return false;
        }

        #endregion
    }
}
