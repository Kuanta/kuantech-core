using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class VenueNpcSpawner : MonoBehaviour
    {
        [SerializeField] private List<ArcadeIdleNpc> NpcsToSpawn;

        public ArcadeIdleNpc SpawnNpc(ArcadeIdleVenue venue)
        {
            int randomIndex = Random.Range(0, NpcsToSpawn.Count);
            ArcadeIdleNpc spawnedNpc = Instantiate(NpcsToSpawn[randomIndex]).GetComponent<ArcadeIdleNpc>();
            Utils.WorldPoint spawnPoint = new Utils.WorldPoint()
            {
                Position = transform.position,
                Rotation = transform.rotation,
            };
            spawnedNpc.Spawn(venue, spawnPoint);
            spawnedNpc.transform.position = transform.position;
            spawnedNpc.transform.rotation = transform.rotation;
            spawnedNpc.transform.localScale = Vector3.one;
            return spawnedNpc;
        }
    }
}