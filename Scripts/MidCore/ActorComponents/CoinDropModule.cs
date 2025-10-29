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
        public string CoinDropPropConfigKey = "CoinDropProbability";
        public float CoinDropProbability = 0.75f;
        [SerializeField] private CoinDrop CoinDrop;
        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if (!CoinDropPropConfigKey.IsNullOrEmpty())
            {
                CoinDropProbability = ConfigManager.GetFloatConfig(CoinDropPropConfigKey, CoinDropProbability);
            }
            if (newState == ActorState.Dead)
            {
                if (Random.Range(0f, 1f) > CoinDropProbability)
                {
                    return;
                }
                
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