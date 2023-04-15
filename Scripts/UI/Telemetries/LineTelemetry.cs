using DTT.AreaOfEffectRegions;

namespace Kuantech.Core.UI
{
    public class LineTelemetry : Telemetry
    {
        public LineRegion LineRegion;
        public override void SetLength(float length)
        {
            LineRegion.Length = length;
        }

        public override void SetFill(float fill)
        {
            LineRegion.FillProgress = fill;
        }

        public override void SetAngle(float angle)
        {
            LineRegion.Angle = angle;
        }

        public override void SetWidth(float width)
        {
            LineRegion.Width = width;
        }
    }
}