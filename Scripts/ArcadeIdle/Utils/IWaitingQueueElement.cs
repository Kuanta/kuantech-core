using Kuantech.Utils;
using Kuantech.Utils.Math;

namespace Kuantech.ArcadeIdle
{
    public interface IWaitingQueueElement
    {
        public void GoToPosition(WorldPoint worldPoint);
        public void WarpToPosition(WorldPoint worldPoint);
        public float GetSize();
        
        #region Spline Waiting Queue
        public void SetSpline(BSpline spline){}

        public SplineFollower GetSplineFollower()
        {
            return null;
        }

        public void GoToSplineDistance(float distance)
        {
            
        }
        #endregion
    }
}