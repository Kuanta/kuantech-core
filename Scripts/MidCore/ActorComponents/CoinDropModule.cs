using Kuantech.Core;
using Kuantech.Core.Store;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// A very simple coin drop module that spawns a coin when the actor dies.
    /// </summary>
    public class CoinDropModule : ActorModule
    {
        public CurrencyData CurrencyData;
        [SerializeField] private CoinDrop CoinDrop;
        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if (newState == ActorState.Dead)
            {
                CoinDrop coinDrop = PoolManager.GetObjectFromPool(CoinDrop.gameObject).GetComponent<CoinDrop>();
                if (coinDrop == null) return;
                coinDrop.Drop(new WorldPoint()
                {
                    Position = transform.position,
                }, CurrencyData);
            }
        }
    }
}