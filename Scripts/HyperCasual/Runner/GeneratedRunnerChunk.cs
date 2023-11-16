using Kuantech.Core.Utils;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class GeneratedRunnerChunk : RunnerChunk
    {
        public PowerLevelArray<GameObject> ChunkElements;

        public override void Initialize(RunnerLevel parentLevel, bool isFinalChunk = false)
        {
            if (ChunkElements == null)
            {
                base.Initialize(parentLevel, isFinalChunk);
                return;
            };
            GameObject elementsPrefab = ChunkElements.GetAvailableElements(parentLevel.PowerLevel).GetRandomElement();
            if(elementsPrefab != null)
            {
                GameObject elements = Instantiate(elementsPrefab);
                elements.AttachToParent(transform);
            }
            base.Initialize(parentLevel, isFinalChunk);
        }

    }
}