namespace Kuantech.Core.HyperCasual.Runner
{
    public interface IChunkElement
    {
        public void OnPreChunkGenerated(RunnerChunk chunk){}

        public void OnChunkGenerated(RunnerChunk chunk);

        public void OnChunkRestart();
        
        public void OnPlayerEnteredChunk();
        
        public void OnPlayerExitedChunk();

        public void OnClearChunk();
    }
}