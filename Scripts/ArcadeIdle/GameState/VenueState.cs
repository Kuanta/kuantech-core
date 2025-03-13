using System;
using System.Collections.Generic;
using Kuantech.Core;

namespace Kuantech.ArcadeIdle
{
    [Serializable]
    public class VenueState
    {
        [NonSerialized] public bool Dirtied = false;
        public Dictionary<string, ActorSerializableData> VenueActorStates;
        public Dictionary<string, bool> ZoneStates;
        public List<CharacterState> WorkerStates;
    }
}