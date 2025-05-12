using UnityEngine;

namespace Kuantech.Core
{
    public abstract class StateModel : ScriptableObject
    {
        public virtual string ModuleID => GetType().FullName;

        public bool Dirtied = false;
        
        public virtual void OnRegistered(){}
        public virtual void SetDefaultValues(){}

        public abstract byte[] Serialize();
        public abstract void Deserialize(byte[] data);
    }
}