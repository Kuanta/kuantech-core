using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kuantech.Core
{
    public class PrefabPool
    {
        private readonly Transform _poolParent;
        private Dictionary<GameObject, Queue<GameObject>> _pool;
        private Dictionary<GameObject, int> _poolSizeLevels; // Not actual sizes, but the size levels
        private readonly int _size;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="poolParent"> Parent to store inactive gameobjects</param>
        /// <param name="size"> Max size for each sub-pool</param>
        public PrefabPool(Transform poolParent, int size)
        {
            _size = size;
            _poolParent = poolParent;
            _pool = new Dictionary<GameObject, Queue<GameObject>>();
            _poolSizeLevels = new Dictionary<GameObject, int>();
        }

        /// <summary>
        /// Pools an object if it has poolable component. Destroys it otherwise.
        /// </summary>
        /// <param name="objectToPool">Object to be pooled</param>
        public void PoolObject(GameObject objectToPool)
        {
            if (!objectToPool.TryGetComponent(out PoolableComponent poolable))
            {
                Debug.LogError($"Prefab {objectToPool.name} doesn't have poolable component");
                UnityEngine.Object.Destroy(objectToPool);
                return;
            }

            GameObject key = poolable.CorrespondingPrefab;
            if (objectToPool == null) return;
        
            if (!_pool.ContainsKey(key)) //This should never be the case
            {
                Debug.LogError($"Prefab {objectToPool.name} doesn't have a field in the pool");
                UnityEngine.Object.Destroy(objectToPool);
                return;
            }
            
            if (_pool[key].Count >= _size)
            {
                UnityEngine.Object.Destroy(objectToPool); // Pool is full, no need to store anymore
            }
            else
            {
                objectToPool.SetActive(false);
                objectToPool.transform.SetParent(_poolParent, false);
                poolable.InUse = false;
                _pool[key].Enqueue(objectToPool);
            }
        }

        /// <summary>
        /// Returns an instantiated object from given prefab id that is only valid in this pool's context
        /// </summary>
        /// <param name="prefab">Key of the object</param>
        /// <returns></returns>
        public GameObject GetObject(GameObject prefab)
        {
            if (!_pool.ContainsKey(prefab))
            {
                InsertField(prefab); //Register the field
            }
            if (_pool[prefab].Count > 0)
            {
                GameObject dequeued = _pool[prefab].Dequeue();
                if (dequeued == null)
                {
                    Debug.LogError("Pooled object is null");
                    return CreateNew(prefab);
                }
                dequeued.SetActive(true);
                dequeued.transform.parent = null;
                dequeued.GetComponent<PoolableComponent>().InUse = true;
                return dequeued;
            }
            
            // Key exists but not enough instances stored
            ExtendSubPool(prefab);  //Extend the corresponding pool
            return CreateNew(prefab, true);
        }

        /// <summary>
        /// Instantiates and returns a new object. Also adds poolable component so that the object can be pooled to
        /// corresponding field when its lifecycle ends
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <returns></returns>
        private GameObject CreateNew(GameObject prefab, bool inUse = true)
        {
            GameObject newObject = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            PoolableComponent objectPoolableComponent = newObject.AddComponent<PoolableComponent>();
            objectPoolableComponent.InUse = inUse;
            objectPoolableComponent.CorrespondingPrefab = prefab;
            return newObject;
        }
        
        private void InsertField(GameObject key)
        {
            _pool.Add(key, new Queue<GameObject>());
            _poolSizeLevels.Add(key, 0);
        }
        
        /// <summary>
        /// Extends a sub pool using the Fibonacci series. A level counter for each subpool is stored. Each time the pool
        /// is empty, the level is incremented by 1 for that pool and 'Fibonacci(level)' objects are generated.
        /// </summary>
        /// <param name="key">Key of the subpool</param>
        private void ExtendSubPool(GameObject key)
        {
            if (!_poolSizeLevels.ContainsKey(key))
            {
                // Should never happen
                Debug.LogError($"{key} key doesn't exist in size levels");
            }
            _poolSizeLevels[key]++;
            int objectsToCreate = GetSizeFromLevel(_poolSizeLevels[key]);
            
            // Create the prefabs but don't exceed the max limit
            for (int i = _pool[key].Count; i < objectsToCreate && i < _size; i++)
            {
                GameObject instanced = CreateNew(key, false);
                PoolObject(instanced);
            }
        }
        /// <summary>
        /// Clears the pool, destroys all pooled objects
        /// </summary>
        public void Clear()
        {
            foreach (GameObject subPoolKey in _pool.Keys)
            {
                foreach (GameObject pooledObject in _pool[subPoolKey])
                {
                    UnityEngine.Object.Destroy(pooledObject);
                }
                _pool[subPoolKey].Clear();
            }
            _pool.Clear();
            _poolSizeLevels.Clear();
        }
        
        /// <summary>
        /// Returns the corresponding fibonacci number
        /// </summary>
        /// <param name="level">Index of the fibonacci serie</param>
        /// <returns></returns>
        private int GetSizeFromLevel(int level)
        {
            level = Mathf.Max(0, level);
            return Mathf.Min(Helpers.FibonacciNumber(level), _size);
        }
        
    }
}