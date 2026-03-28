using System;
using System.Collections.Generic;

namespace Kuantech.Core
{  
    public class SaveDataState
    {
        [NonSerialized] public bool Dirtied = false;
    }

    [Serializable]
    public class ActorSerializableData : SaveDataState
    {
        public string ActorId;
        public string ActorVisualId;
        public Dictionary<string, ActorModuleSerializableData> ModuleStates;
    }

    [Serializable]
    public class ActorModuleSerializableData : SaveDataState
    {
        public string ModuleId;
    }
}