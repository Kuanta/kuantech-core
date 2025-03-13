using Kuantech.Core;

namespace Kuantech.ArcadeIdle
{
    public class VenueActorSerializableData : ActorSerializableData
    {
        public bool Locked;

        public void ToggleLockedState(bool locked)
        {
            Locked = locked;
        }
    }
}