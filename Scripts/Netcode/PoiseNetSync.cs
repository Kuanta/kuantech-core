using Unity.Netcode;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// The NGO half of a PoiseModule: carries the server's poise break start/end to every other peer.
    /// Sits on the same NetworkObject as the Actor.
    /// </summary>
    public class PoiseNetSync : NetworkBehaviour, IActorNetBinding
    {
        private PoiseModule _module;

        void IActorNetBinding.Bind(Actor actor)
        {
            _module = actor.GetModule<PoiseModule>();
            if (_module == null)
            {
                Debug.LogWarning($"[PoiseNetSync] {name} has no PoiseModule to sync.");
                return;
            }

            // Unsubscribe first so a repeated Bind (a recycled actor) never doubles the RPCs.
            _module.OnPoiseBreakBroadcast -= BroadcastPoiseBreak;
            _module.OnPoiseBreakBroadcast += BroadcastPoiseBreak;
        }

        private void BroadcastPoiseBreak(bool broken)
        {
            if (IsSpawned) ObserversPoiseBreak_Rpc(broken);
        }

        // The authority already ran the break itself before broadcasting.
        [Rpc(SendTo.NotServer)]
        private void ObserversPoiseBreak_Rpc(bool broken)
        {
            if (_module != null) _module.ReceivePoiseBreak(broken);
        }
    }
}
