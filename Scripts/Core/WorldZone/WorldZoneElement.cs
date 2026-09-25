using UnityEngine;

namespace Kuantech.Core
{
    public class WorldZoneElement : MonoBehaviour
    {
        //Runtime
        private WorldZone _parentZone;
        public WorldZone ParentZone => _parentZone;

        public virtual void Initialize(WorldZone zone)
        {
            _parentZone = zone;
        }

        public virtual void OnZoneActivated()
        {
            
        }

        public virtual void OnZoneDeactivated()
        {
            
        }

        public virtual void UpdateZoneElement(float deltaTime, bool zoneActive)
        {
            
        }
        public virtual void CleanupZone()
        {
            
        }

        public virtual void ResetZone()
        {
            
        }
    }
}