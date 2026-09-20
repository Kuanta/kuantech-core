using System;
using Kuantech.Core;
using UnityEngine;
using TMPro;

#if NETWORKING_NGO
using Unity.Netcode;
#endif

namespace Kuantech.Networking
{
    /// <summary>
    /// Carries a player's replicated identity on the player itself. The server fills it in at spawn from
    /// the match roster; everyone else reads it.
    ///
    /// It lives on the actor rather than in one central list on purpose. A NetworkVariable rides along in
    /// the object's spawn payload, so any client that starts observing this player later already has the
    /// value -- no push-to-late-joiner bookkeeping, which is exactly the problem KtNetworkManager has to
    /// solve by hand for state that travels as messages. State belongs in variables, events in messages.
    /// </summary>
    public class PlayerIdentityModule : ActorModule
    {
        [SerializeField] private TMP_Text PlayerNameText;
#if NETWORKING_NGO
        private readonly NetworkVariable<PlayerLoadoutData> _loadout = new NetworkVariable<PlayerLoadoutData>();
#else
        private readonly OfflineNetworkVariable<PlayerLoadoutData> _loadout = new OfflineNetworkVariable<PlayerLoadoutData>();
#endif

        /// <summary>Raised on every peer once this player's identity is known, and again if it changes.</summary>
        public event Action<PlayerLoadoutData> LoadoutChanged;

        public PlayerLoadoutData Loadout => _loadout.Value;

        public string DisplayName => _loadout.Value.PlayerName.ToString();

        /// <summary>
        /// Server only. Called by MatchManager right after the player is spawned.
        /// </summary>
        public void ApplyLoadout(PlayerLoadoutData loadout)
        {
            if (!IsServerInitialized)
            {
                Debug.LogWarning($"[PlayerIdentityModule] ApplyLoadout called on a non-server peer for '{name}' -- ignored.");
                return;
            }
            _loadout.Value = loadout;
        }

#if NETWORKING_NGO
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _loadout.OnValueChanged += OnLoadoutValueChanged;

            // Both paths are needed, and which one fires depends on timing we do not control. A client
            // that arrives after the server has already set this gets the value inside the spawn payload
            // and no change callback at all; one that is already watching gets the callback instead.
            if (!_loadout.Value.IsEmpty) Announce(_loadout.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _loadout.OnValueChanged -= OnLoadoutValueChanged;
        }
#endif

        private void OnLoadoutValueChanged(PlayerLoadoutData previous, PlayerLoadoutData current)
        {
            Announce(current);
        }

        // Lives here rather than in the change callback because that callback is only one of the two ways
        // this value arrives. A client that starts observing a player who already has a loadout receives it
        // inside the spawn payload, with no change callback at all -- and that player's nameplate would
        // stay blank. Both paths run Announce, so this is the one place that catches both.
        private void RefreshNameplate(PlayerLoadoutData loadout)
        {
            if (PlayerNameText == null) return;

            // Your own name over your own head is just clutter -- everyone else needs it, you do not.
            if (IsOwnerLocal())
            {
                PlayerNameText.gameObject.SetActive(false);
                return;
            }

            PlayerNameText.text = loadout.PlayerName.ToString();
        }

        private void Announce(PlayerLoadoutData loadout)
        {
            Debug.Log($"[PlayerIdentityModule] '{name}' is {loadout} " +
                      $"(local player: {IsOwnerLocal()}).");
            RefreshNameplate(loadout);
            LoadoutChanged?.Invoke(loadout);
        }

        private bool IsOwnerLocal()
        {
#if NETWORKING_NGO
            return IsOwner;
#else
            return true;
#endif
        }
    }
}
