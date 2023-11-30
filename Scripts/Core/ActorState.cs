using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kuantech.Core
{  
    public class SaveDataState
    {
        [NonSerialized] public bool Dirtied = false;
        public virtual string EncodeState()
        {
            Dirtied = false;
            return JsonConvert.SerializeObject(this);
        }
        public virtual void DecodeState(string savedData)
        {
            if(savedData == null) return;
            var loadedObject = JsonConvert.DeserializeObject(savedData, this.GetType());
            foreach (var property in this.GetType().GetFields())
            {
                var loadedValue = property.GetValue(loadedObject);
                property.SetValue(this, loadedValue);
            }
        }
    }

    [Serializable]
    public class ActorState : SaveDataState
    {
        public string ActorId;
        public Dictionary<string, string> EncodedModuleStates;
        [NonSerialized] public Dictionary<string, ActorModuleState> ModuleStates; 

        /// <summary>
        /// Loads the state of the actor
        /// </summary>
        /// <param name="savedData"></param>
        public override void DecodeState(string savedData)
        {
            base.DecodeState(savedData);
            ModuleStates = new Dictionary<string, ActorModuleState>();
        }
        
        /// <summary>
        /// Saves the state of the actor
        /// </summary>
        /// <returns></returns>
        public override string EncodeState()
        {
            base.EncodeState();
            Dirtied = false;
            foreach(var pair in ModuleStates)
            {
                EncodedModuleStates[pair.Key] = pair.Value.EncodeState();
            }
            return JsonConvert.SerializeObject(this);
        }
    }

    [Serializable]
    public class ActorModuleState : SaveDataState
    {
        public string ModuleId;
    }
}