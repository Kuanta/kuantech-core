using System;
using System.Collections.Generic;

namespace Kuantech.AI
{
    [Serializable]
    public class BehaviourTree : BTNode
    {
        public BTVariableTable VariableTable;
        [NonSerialized] public BTAgent OwnerAgent;
        
        public BehaviourTree()
        {
            Name = "Root";
            Owner = this;
        }

        public BehaviourTree(string n)
        {
            Name = n;
            Owner = this;
        }

        public override NodeStatus Process()
        {
            return GetCurrentChild().Process();
        }
        // Get a variable. Note that the caller should know the type and cast accordingly.
        public object GetVariable(string key)
        {
            return VariableTable.GetVariable(key);
        }

        // Helper method to get a variable and cast to a specific type.
        public T GetVariable<T>(string key)
        {
           return VariableTable.GetVariable<T>(key);
        }
    }

    public class BTVariableTable
    {
        private Dictionary<string, object> _table = new Dictionary<string, object>();
        // Register a variable
        public void RegisterVariable(string key, object value)
        {
            if (_table.ContainsKey(key))
            {
                _table[key] = value; // Update the existing value
            }
            else
            {
                _table.Add(key, value); // Add new key-value pair
            }
        }
        
        public void UnregisterVariable(string key)
        {
            if (_table.ContainsKey(key))
            {
                _table.Remove(key);
            }
        }

        // Get a variable. Note that the caller should know the type and cast accordingly.
        public object GetVariable(string key)
        {
            _table.TryGetValue(key, out var value);
            return value;
        }
        
        // Helper method to get a variable and cast to a specific type.
        public T GetVariable<T>(string key)
        {
            object value;
            _table.TryGetValue(key, out value);
            if (value is T castedValue)
            {
                return castedValue;
            }
            return default(T); // or throw an exception, based on your use case
        }

        public void ClearTable()
        {
            if(_table != null) _table.Clear();
        }
    }
}