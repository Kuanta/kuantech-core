using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

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
            if(loadedObject == null) return;
            foreach (var property in this.GetType().GetFields())
            {
                if(property.IsDefined(typeof(NonSerializedAttribute), false)) continue;
                try{
                    var loadedValue = property.GetValue(loadedObject);
                    property.SetValue(this, loadedValue);
                }catch(TargetException targetExp)
                {
                    Debug.LogError(targetExp.Message);
                    break;
                }
                
            }
        }
    }

    [Serializable]
    public class ActorState : SaveDataState
    {
        [NonSerialized] public Actor Actor;
        public Dictionary<string, string> EncodedModuleStates;
        
        /// <summary>
        /// Saves the state of the actor
        /// </summary>
        /// <returns></returns>
        public override string EncodeState()
        {
            base.EncodeState();
            Dirtied = false;
            if(Actor == null)
            {
                Debug.LogError("Actor is null you fok face!");
                return "";
            }
            foreach(var pair in Actor.ModulesById)
            {
                if(!pair.Value.CurrentState.Dirtied) continue;
                EncodedModuleStates[pair.Key] = pair.Value.CurrentState.EncodeState();
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