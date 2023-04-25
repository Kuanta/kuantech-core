using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public interface ISpawnable
    {
        public void OnSpawn(Vector3 position, Quaternion rotation);

        public void OnRespawn(Vector3 position, Quaternion rotation);

        public void OnPlayerEnteredChunk();
    }
}