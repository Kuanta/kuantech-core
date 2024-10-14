using Kuantech.Utils;

namespace Kuantech.ArcadeIdle
{
    public interface IWaitingQueueElement
    {
        public void GoToPosition(WorldPoint worldPoint);
        public void WarpToPosition(WorldPoint worldPoint);
        public float GetSize();
    }
}