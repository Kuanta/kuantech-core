using System;
using System.Collections.Generic;

namespace Kuantech.Core
{  
    public class SaveDataState
    {
        [NonSerialized] public bool Dirtied = false;
    }

    [Serializable]
    public class ActorState : SaveDataState
    {
        public string ActorId;
        public Dictionary<string, ActorModuleState> ModuleStates;
    }

    [Serializable]
    public class ActorModuleState : SaveDataState
    {
        public string ModuleId;
    }
}