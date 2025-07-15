using UnityEngine;

namespace Kuantech.Core
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindObjectOfType<T>();
                if (_instance != null) return _instance;
                GameObject obj = new GameObject
                {
                    name = typeof(T).Name
                };
                _instance = obj.AddComponent<T>();

                return _instance;
            }
        }

        public static bool InstanceExists()
        {
            if(_instance != null) return true;
            _instance = FindObjectOfType<T>();
            return _instance != null;
        }
    }
}