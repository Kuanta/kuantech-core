using Kuantech.Utils;

namespace Kuantech.ArcadeIdle
{
    public interface IResourcePositioner
    {
        public void GoToTarget(WorldPoint targetPoint);

        public void WarpToPoint(WorldPoint targetPoint);
    }
}