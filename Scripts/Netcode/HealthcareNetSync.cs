using Kuantech.Rpg;
using Kuantech.Rpg.Managers;
using Unity.Netcode;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// The NGO half of a HealthcareModule: turns the module's outbound events into RPCs and hands what
    /// arrives back to the module's receive methods. Sits on the same NetworkObject as the Actor.
    /// </summary>
    public class HealthcareNetSync : NetworkBehaviour, IActorNetBinding
    {
        private HealthcareModule _module;

        void IActorNetBinding.Bind(Actor actor)
        {
            _module = actor.GetModule<HealthcareModule>();
            if (_module == null)
            {
                Debug.LogWarning($"[HealthcareNetSync] {name} has no HealthcareModule to sync.");
                return;
            }

            // Unsubscribe first so a repeated Bind (a recycled actor) never doubles the RPCs.
            _module.OnResourceValueBroadcast -= BroadcastResourceValue;
            _module.OnResourceValueBroadcast += BroadcastResourceValue;
            _module.OnResourceRefreshBroadcast -= BroadcastResourceRefresh;
            _module.OnResourceRefreshBroadcast += BroadcastResourceRefresh;
            _module.OnRefreshBroadcast -= BroadcastRefresh;
            _module.OnRefreshBroadcast += BroadcastRefresh;
            _module.OnHitAnimBroadcast -= BroadcastHitAnim;
            _module.OnHitAnimBroadcast += BroadcastHitAnim;
        }

        #region Outbound

        private void BroadcastResourceValue(ResourceAsset resourceAsset, float value)
        {
            if (IsSpawned) ObserverSyncResource_Rpc(resourceAsset.Id, value);
        }

        private void BroadcastResourceRefresh(ResourceAsset resourceAsset)
        {
            if (IsSpawned) ObserversRefreshResource_Rpc(resourceAsset.Id);
        }

        private void BroadcastRefresh()
        {
            if (IsSpawned) ObserversRefresh_Rpc();
        }

        private void BroadcastHitAnim(HitInfo hitInfo)
        {
            if (IsSpawned) ObserversHitAnim_Rpc(hitInfo);
        }

        #endregion

        #region Inbound

        // Authoritative value sync — the server sends the final value, clients just apply it
        [Rpc(SendTo.NotServer)]
        private void ObserverSyncResource_Rpc(string resourceId, float resourceValue)
        {
            ResourceAsset resourceAsset = RpgManager.GetResourceAssetById(resourceId);
            if (_module == null || resourceAsset == null) return;
            _module.ReceiveResourceValue(resourceAsset, resourceValue);
        }

        // Refresh to max — deterministic, safe to re-run on clients
        [Rpc(SendTo.NotServer)]
        private void ObserversRefreshResource_Rpc(string resourceId)
        {
            ResourceAsset resourceAsset = RpgManager.GetResourceAssetById(resourceId);
            if (_module == null || resourceAsset == null) return;
            _module.ReceiveResourceRefresh(resourceAsset);
        }

        [Rpc(SendTo.NotServer)]
        private void ObserversRefresh_Rpc()
        {
            if (_module != null) _module.ReceiveRefresh();
        }

        // Every peer presents the hit, but the authority already ran it directly before broadcasting, so a
        // copy sent back to it (the host's screen) would play the reaction twice -- hence NotServer.
        [Rpc(SendTo.NotServer)]
        private void ObserversHitAnim_Rpc(HitInfo hitInfo)
        {
            if (_module != null) _module.PresentHitAnim(hitInfo);
        }

        #endregion
    }
}
