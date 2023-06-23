using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public abstract class RunnerChunkSlot : MonoBehaviour
    {
        public abstract void OnAssign(RunnerChunk chunk, int row, int col, int maxRow, int maxCol,
            string customParameter = null);
    }
}