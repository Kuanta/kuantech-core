namespace Kuantech.Core.HyperCasual
{
    public interface IChunkElement
    {
        public void OnChunkGenerated(RunnerChunk chunk);

        public void OnChunkRestart();
        
        public void OnPlayerEnteredChunk();
        public void OnPlayerExitedChunk();

        public void OnClearChunk();
    }
}