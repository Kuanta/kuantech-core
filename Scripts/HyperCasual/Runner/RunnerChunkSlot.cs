using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public abstract class RunnerChunkSlot : MonoBehaviour
    {
        public abstract void OnAssign(RunnerChunk chunk, int row, int col, int maxRow, int maxCol,
            string customParameter = null);

        protected List<string> ParseArgument(string customParameter)
        {
            return customParameter.Split(',').ToList();
        }
    }
}