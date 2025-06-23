using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class VariableTable
    {
        private Dictionary<string, object> _variables;

        public VariableTable()
        {
            _variables = new Dictionary<string, object>();
        }

        public void RegisterVariable(string name, object variable)
        {
            if (_variables.ContainsKey(name))
            {
                return;
            }
            
            _variables.Add(name, variable);
        }

        public T GetVariable<T>(string variableName)
        {
            if (!_variables.ContainsKey(variableName))
            {
                return default(T);
            }

            object variable = _variables[variableName];
            try
            {
                return (T) variable;
            }
            catch (InvalidCastException e)
            {
                return default(T);
            }
        }
    }
}