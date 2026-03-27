namespace Kuantech.World
{
    public interface IZoneElement
    {
        Zone Zone { get; }
        void Initialize(Zone zone);
        void OnZoneActivated();
        void OnZoneDeactivated();
    }
}
