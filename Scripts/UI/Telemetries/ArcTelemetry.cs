using DTT.AreaOfEffectRegions;

namespace Kuantech.Core.UI
{
    public class ArcTelemetry : Telemetry
    {
        public ArcRegion ArcRegion;
        public override void SetLength(float length)
        {
            ArcRegion.Radius = length;
        }

        public override void SetFill(float fill)
        {
            ArcRegion.FillProgress = fill;
        }

        public override void SetAngle(float angle)
        {
            ArcRegion.Arc = angle;
        }
    }
}