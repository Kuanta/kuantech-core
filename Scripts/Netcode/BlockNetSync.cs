using Unity.Netcode;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// The NGO half of a BlockModule: carries the controlling peer's block requests to the server, the
    /// server's relay to everyone else, and the "clang" cue of a blocked hit. Sits on the same NetworkObject
    /// as the Actor.
    /// </summary>
    public class BlockNetSync : NetworkBehaviour, IActorNetBinding
    {
        private BlockModule _module;

        void IActorNetBinding.Bind(Actor actor)
        {
            _module = actor.GetModule<BlockModule>();
            if (_module == null)
            {
                Debug.LogWarning($"[BlockNetSync] {name} has no BlockModule to sync.");
                return;
            }

            // Unsubscribe first so a repeated Bind (a recycled actor) never doubles the RPCs.
            _module.OnStartBlockRequested -= RequestStartBlock;
            _module.OnStartBlockRequested += RequestStartBlock;
            _module.OnEndBlockRequested -= RequestEndBlock;
            _module.OnEndBlockRequested += RequestEndBlock;
            _module.OnStartBlockBroadcast -= BroadcastStartBlock;
            _module.OnStartBlockBroadcast += BroadcastStartBlock;
            _module.OnEndBlockBroadcast -= BroadcastEndBlock;
            _module.OnEndBlockBroadcast += BroadcastEndBlock;
            _module.OnBlockedHitBroadcast -= BroadcastBlockedHit;
            _module.OnBlockedHitBroadcast += BroadcastBlockedHit;
        }

        #region Outbound

        private void RequestStartBlock()
        {
            if (IsSpawned) ServerStartBlock_Rpc();
        }

        private void RequestEndBlock()
        {
            if (IsSpawned) ServerEndBlock_Rpc();
        }

        private void BroadcastStartBlock()
        {
            if (IsSpawned) ObserversStartBlock_Rpc();
        }

        private void BroadcastEndBlock()
        {
            if (IsSpawned) ObserversEndBlock_Rpc();
        }

        private void BroadcastBlockedHit()
        {
            if (IsSpawned) ObserversBlockedHit_Rpc();
        }

        #endregion

        #region Inbound

        [Rpc(SendTo.Server)]
        private void ServerStartBlock_Rpc()
        {
            if (_module != null) _module.ReceiveStartBlockRequest();
        }

        [Rpc(SendTo.Server)]
        private void ServerEndBlock_Rpc()
        {
            if (_module != null) _module.ReceiveEndBlockRequest();
        }

        // NotOwner also reaches the server when it is not the owner; the module's guards keep it from
        // running a block it just executed itself a second time.
        [Rpc(SendTo.NotOwner)]
        private void ObserversStartBlock_Rpc()
        {
            if (_module != null) _module.ReceiveStartBlock();
        }

        [Rpc(SendTo.NotOwner)]
        private void ObserversEndBlock_Rpc()
        {
            if (_module != null) _module.ReceiveEndBlock();
        }

        // The authority already played the cue itself before broadcasting.
        [Rpc(SendTo.NotServer)]
        private void ObserversBlockedHit_Rpc()
        {
            if (_module != null) _module.PresentBlockedHit();
        }

        #endregion
    }
}
