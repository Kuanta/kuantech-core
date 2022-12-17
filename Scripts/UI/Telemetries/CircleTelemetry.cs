using DTT.AreaOfEffectRegions;

namespace Kuantech.Core.UI
{
    public class CircleTelemetry : Telemetry
    {
        public CircleRegion CircleRegion;
        
        public override void SetLength(float length)
        {
            CircleRegion.Radius = length;
        }

        public override void SetFill(float fill)
        {
            CircleRegion.FillProgress = fill;
        }

        public override void SetAngle(float angle)
        {
        }
    }
}