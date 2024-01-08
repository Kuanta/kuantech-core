using Kuantech.Core;

namespace Kuantech.ArcadeIdle
{
    public class VenueActorState : ActorState
    {
        public bool Locked;

        public void ToggleLockedState(bool locked)
        {
            Locked = locked;
        }
    }
}