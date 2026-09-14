using System;
#if NETWORKING_NGO
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
#endif
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// Broadcasts arena-wide events (wave start, ...) to every connected client via NGO's
    /// CustomMessagingManager -- not an RPC, so nothing needs to be (or live on) a NetworkObject. The arena
    /// stays exactly what it looks like: a plain scene object whose spawn logic only ever runs on the
    /// server; clients never see or touch it, same as it would with Mirror's NetworkServer.SendToAll.
    /// </summary>
    public static class WaveAnnouncer
    {
        private const string MessageName = "WaveStarted";
        public static event Action<int> OnWaveStarted;

#if NETWORKING_NGO
        // Tracks which NetworkManager instance we last registered on (not just "have we ever registered") --
        // a bare bool would stay true forever after the first registration even if NetworkManager.Singleton
        // gets torn down and recreated (a menu->game scene transition, a reconnect), silently leaving the
        // CURRENT instance with no handler at all while looking "already done".
        private static NetworkManager _listeningOn;

        /// <summary>
        /// Registers this peer's message handler on the CURRENT NetworkManager.Singleton. Safe to call
        /// repeatedly/every frame (idempotent against re-registering on the same instance, and against
        /// calling before the network has actually started) -- CustomMessagingManager is only constructed
        /// once StartHost/StartClient/StartServer actually runs, NOT as soon as the NetworkManager component
        /// itself exists in the scene, so this no-ops (rather than throwing) until that's happened. Call
        /// this before subscribing to OnWaveStarted (GameHUD does) so the handler is armed before any
        /// message can arrive.
        /// </summary>
        public static void EnsureListening()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.CustomMessagingManager == null) return;
            if (_listeningOn == NetworkManager.Singleton) return;
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(MessageName, OnMessageReceived);
            _listeningOn = NetworkManager.Singleton;
            Debug.Log($"[WaveAnnouncer] EnsureListening: registered on '{NetworkManager.Singleton.name}' (IsServer={NetworkManager.Singleton.IsServer}, IsClient={NetworkManager.Singleton.IsClient}).");
        }

        private static void OnMessageReceived(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int waveIndex);
            Debug.Log($"[WaveAnnouncer] OnMessageReceived: waveIndex={waveIndex} from senderClientId={senderClientId}.");
            OnWaveStarted?.Invoke(waveIndex);
        }
#endif

        /// <summary>Server-only. Broadcasts to every other connected client and fires locally right away for
        /// the host's own UI. Deliberately excludes the local client id from the send list rather than using
        /// SendNamedMessageToAll -- on a host, NGO loops that back into a local handler invocation too, which
        /// combined with the explicit call below would fire OnWaveStarted twice.</summary>
        public static void AnnounceWaveStarted(int waveIndex)
        {
#if NETWORKING_NGO
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            List<ulong> remoteClients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
            remoteClients.Remove(NetworkManager.Singleton.LocalClientId);

            Debug.Log($"[WaveAnnouncer] AnnounceWaveStarted: waveIndex={waveIndex}, remoteClients=[{string.Join(",", remoteClients)}].");

            if (remoteClients.Count > 0)
            {
                using (FastBufferWriter writer = new FastBufferWriter(sizeof(int), Allocator.Temp))
                {
                    writer.WriteValueSafe(waveIndex);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(MessageName, remoteClients, writer);
                }
            }
#endif
            OnWaveStarted?.Invoke(waveIndex);
        }

        /// <summary>
        /// Server-only. Catches up ONE client on the currently active wave -- SendNamedMessage is fire-and-
        /// forget, so a client that connects after a wave already started never saw that broadcast and would
        /// otherwise show nothing until (if ever) the next wave begins. Call this from the server's
        /// OnClientConnectedCallback.
        /// </summary>
        public static void AnnounceWaveStartedTo(int waveIndex, ulong clientId)
        {
#if NETWORKING_NGO
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            // The host "connecting" fires this too, for its own local client id -- it already got the
            // original broadcast (or will, via the normal AnnounceWaveStarted path), no catch-up needed.
            if (clientId == NetworkManager.Singleton.LocalClientId) return;

            using (FastBufferWriter writer = new FastBufferWriter(sizeof(int), Allocator.Temp))
            {
                writer.WriteValueSafe(waveIndex);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(MessageName, clientId, writer);
            }
#endif
        }
    }
}
