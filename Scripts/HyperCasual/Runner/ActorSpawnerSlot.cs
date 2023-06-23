using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class ActorSpawnerSlot : RunnerChunkSlot
    {
        [SerializeField] private Actor Spawnable;
        public override void OnAssign(RunnerChunk chunk, int row, int col, int maxRow, int maxCol, string customParameter = null)
        {
            if (Spawnable != null)
            {
                chunk.ParentLevel.SpawnSpawnable(Spawnable);
            }
        }
    }
}