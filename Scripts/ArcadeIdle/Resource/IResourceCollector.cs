namespace Kuantech.ArcadeIdle
{
    public interface IResourceCollector
    {
        public bool CanCollectResource(string resourceId);
        public void CollectResource(string resourceId, int amount = 1);
    }
}