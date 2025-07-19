using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core
{

    public class ModuleHandler<TBase> where TBase : MonoBehaviour
    {
        private readonly Dictionary<Type, TBase> _modules = new();

        public void AddModule<T>(T module) where T : TBase
        {
            var type = typeof(T);
            if (!_modules.ContainsKey(type))
            {
                _modules[type] = module;
            }
        }

        public T GetModule<T>() where T : TBase
        {
            if (_modules.TryGetValue(typeof(T), out var module))
            {
                return module as T;
            }
            
            foreach (var kvp in _modules)
            {
                if (kvp.Value is T t)
                    return t;
            }

            return null;
        }

        public bool HasModule<T>() where T : TBase => GetModule<T>() != null;

        public IEnumerable<TBase> GetAllModules() => _modules.Values;
    }

}