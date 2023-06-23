using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public interface ISpawnable
    {
        public void OnSpawn();

        public void OnRespawn();

        public void OnDespawn();
    }
}