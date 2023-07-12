using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class LevelFinisher : MonoBehaviour, IChunkElement
    {
        public RunnerChunk ParentChunk;
        private void OnTriggerEnter(Collider other)
        {
            if (ParentChunk.ParentLevel.CurrentState != LevelState.Playing) return;
            ParentChunk.CompleteChunk();
        }

        public void OnChunkGenerated(RunnerChunk chunk)
        {
            ParentChunk = chunk;
        }

        public void OnChunkRestart()
        {
        }

        public void OnPlayerEnteredChunk()
        {
        }

        public void OnPlayerExitedChunk()
        {
        }

        public void OnClearChunk()
        {
        }
    }
}