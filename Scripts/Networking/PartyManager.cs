using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using UnityEngine;
#if UGS_SERVICES
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
#endif

namespace Kuantech.Networking
{
    /// <summary>
    /// One member of a party, flattened out of whatever backend <see cref="PartyManager"/> talks to.
    /// UI and gameplay code only ever see this -- nothing outside PartyManager references the session
    /// SDK's own types, so callers need no compile guard of their own and the backend stays swappable.
    /// </summary>
    public readonly struct PartyMember
    {
        public readonly string Id;
        public readonly string Name;
        public readonly bool IsLeader;

        public PartyMember(string id, string name, bool isLeader)
        {
            Id = id;
            Name = name;
            IsLeader = isLeader;
        }
    }

    /// <summary>
    /// The party layer: create a party, join one by its code, and track who is in it. Sits on top of
    /// <see cref="UgsManager"/> (every call happens as a signed-in player) and, for now, deliberately
    /// below gameplay networking -- a party here is pure metadata, it starts no Netcode connection.
    /// Bringing the gameplay network up is a later step, and amounts to one extra option at create time.
    /// </summary>
    public class PartyManager : SubManager
    {
        [Header("Party")]
        [Tooltip("Party size cap, the leader included.")]
        public int MaxPlayers = 4;

        [Tooltip("Client-side key tagging every session this game creates. It keeps this game's parties " +
                 "distinguishable from anything else sharing the same UGS project -- it is NOT a filter " +
                 "on who may join, since joining happens by code.")]
        public string SessionType = "castle-defenders-party";

        [Header("Status (runtime read-out)")]
        public string Status = "Not in a party";
        [Tooltip("The code a friend types to join this party. Empty when not in one.")]
        public string JoinCode = "";

        /// <summary>Raised whenever party membership or state changed -- redraw from Members.</summary>
        public event Action PartyChanged;

        /// <summary>Raised with a human-readable reason when a party operation fails.</summary>
        public event Action<string> PartyFailed;

        private readonly List<PartyMember> _members = new List<PartyMember>();
        public IReadOnlyList<PartyMember> Members => _members;

        /// <summary>True while a create/join round-trip is in flight -- disable the buttons on it.</summary>
        public bool IsBusy { get; private set; }

        public static PartyManager Get() => GetContext<PartyManager>();

#if UGS_SERVICES
        private ISession _session;

        public bool InParty => _session != null;
        public bool IsLeader => _session != null && _session.IsHost;
        public int PlayerCount => _session != null ? _session.PlayerCount : 0;
#else
        public bool InParty => false;
        public bool IsLeader => false;
        public int PlayerCount => 0;
#endif

        public override void Cleanup()
        {
            base.Cleanup();
            // Fire-and-forget: Cleanup is synchronous, and a party we fail to leave expires service-side
            // anyway. Leaving explicitly just drops us out of everyone else list right away.
            LeaveParty().Forget();
        }

        #region Party operations

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
            string currentName = AuthenticationService.Instance.PlayerName;
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
                await AuthenticationService.Instance.UpdatePlayerNameAsync(trimmedName);
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

        public async UniTask<bool> CreateParty()
        {
#if UGS_SERVICES
            if (IsBusy) return false;
            IsBusy = true;
            try
            {
                if (!await EnsureSignedIn()) return false;
                if (InParty) await LeaveParty();

                SetStatus("Creating party...");
                SessionOptions options = new SessionOptions
                {
                    MaxPlayers = MaxPlayers,
                    // Hidden from session queries and quick-join: a party is reachable ONLY by its code,
                    // which is the whole point -- nobody stumbles into a friends-only run.
                    IsPrivate = true,
                    Type = SessionType
                }.WithPlayerName();

                AdoptSession(await MultiplayerService.Instance.CreateSessionAsync(options));
                SetStatus($"Party created -- code {JoinCode}");
                return true;
            }
            catch (Exception e)
            {
                Fail("Create party", e);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
#else
            await UniTask.CompletedTask;
            return FailNoDefine("Create party");
#endif
        }

        public async UniTask<bool> JoinParty(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                Fail("Join party", "no code entered");
                return false;
            }
#if UGS_SERVICES
            if (IsBusy) return false;
            IsBusy = true;
            try
            {
                if (!await EnsureSignedIn()) return false;
                if (InParty) await LeaveParty();

                string trimmedCode = code.Trim().ToUpperInvariant();
                SetStatus($"Joining party {trimmedCode}...");

                JoinSessionOptions options = new JoinSessionOptions { Type = SessionType }.WithPlayerName();
                AdoptSession(await MultiplayerService.Instance.JoinSessionByCodeAsync(trimmedCode, options));
                SetStatus($"Joined party {JoinCode}");
                return true;
            }
            catch (Exception e)
            {
                Fail("Join party", e);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
#else
            await UniTask.CompletedTask;
            return FailNoDefine("Join party");
#endif
        }

        /// <summary>
        /// Leader only. A locked party rejects further joins -- called right before a match starts so the
        /// roster cannot grow between snapshotting it and everyone actually connecting.
        /// </summary>
        public async UniTask<bool> LockParty(bool locked)
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
                Fail("Lock party", e);
                return false;
            }
#else
            await UniTask.CompletedTask;
            return FailNoDefine("Lock party");
#endif
        }

        /// <summary>
        /// Leader only. Brings the gameplay network up for the whole party over Relay. The party was
        /// created without one on purpose -- everyone picks a name and a character first, and only then
        /// does anybody connect, so what a client sends on connection is already its final choice.
        /// Members do not call this: their side is brought up by the service once the host's is running.
        /// </summary>
        public async UniTask<bool> StartNetwork()
        {
#if UGS_SERVICES
            if (_session == null || !_session.IsHost)
            {
                Fail("Start network", "only the party leader can start the network");
                return false;
            }
            try
            {
                SetStatus("Starting network...");
                await _session.AsHost().Network.StartRelayNetworkAsync(RelayNetworkOptions.Default);
                SetStatus("Network started");
                return true;
            }
            catch (Exception e)
            {
                Fail("Start network", e);
                return false;
            }
#else
            await UniTask.CompletedTask;
            return FailNoDefine("Start network");
#endif
        }

        public async UniTask LeaveParty()
        {
#if UGS_SERVICES
            if (_session == null) return;

            ISession leaving = _session;
            ClearSession(); // dropped locally first, so the UI reacts even if the call below hangs
            try
            {
                await leaving.LeaveAsync();
                SetStatus("Left the party");
            }
            catch (Exception e)
            {
                Fail("Leave party", e);
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
            session.PlayerPropertiesChanged += OnSessionChanged;
            session.Deleted += OnSessionDeleted;
            session.RemovedFromSession += OnRemovedFromSession;

            RebuildMembers();
            PartyChanged?.Invoke();
        }

        private void ClearSession()
        {
            if (_session != null)
            {
                _session.Changed -= OnSessionChanged;
                _session.PlayerJoined -= OnPlayerJoined;
                _session.PlayerHasLeft -= OnPlayerHasLeft;
                _session.PlayerPropertiesChanged -= OnSessionChanged;
                _session.Deleted -= OnSessionDeleted;
                _session.RemovedFromSession -= OnRemovedFromSession;
                _session = null;
            }

            JoinCode = "";
            _members.Clear();
            PartyChanged?.Invoke();
        }

        /// <summary>
        /// Rebuilt wholesale on every change rather than patched member-by-member: a party is a handful
        /// of entries, and the service can hand us a state we never saw the individual deltas for (a
        /// rejoin, a refresh, a batch of property updates that arrived as one notification).
        /// </summary>
        private void RebuildMembers()
        {
            _members.Clear();
            if (_session == null) return;

            foreach (IReadOnlyPlayer player in _session.Players)
            {
                string playerName = player.GetPlayerName();
                _members.Add(new PartyMember(
                    player.Id,
                    string.IsNullOrEmpty(playerName) ? player.Id : playerName,
                    player.Id == _session.Host));
            }
        }

        private void OnSessionChanged()
        {
            RebuildMembers();
            PartyChanged?.Invoke();
        }

        private void OnPlayerJoined(string playerId) => OnSessionChanged();

        private void OnPlayerHasLeft(string playerId) => OnSessionChanged();

        private void OnSessionDeleted()
        {
            SetStatus("The leader closed the party");
            ClearSession();
        }

        private void OnRemovedFromSession()
        {
            SetStatus("Removed from the party");
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
            Debug.Log($"[PartyManager] {status}");
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
            Debug.LogError($"[PartyManager] {operation} failed: {e}");
        }

        private void Fail(string operation, string reason)
        {
            Status = $"{operation} failed: {reason}";
            PartyFailed?.Invoke(Status);
        }

        private bool FailNoDefine(string operation)
        {
            Fail(operation, "the UGS_SERVICES scripting define is missing, so the party layer is compiled out");
            return false;
        }

        #endregion
    }
}
