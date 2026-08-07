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

        /// <summary>Instances created when a sub pool runs dry. Small enough for one frame to absorb.</summary>
        private const int GrowthStep = 8;
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
        
        private Queue<GameObject> _deferredPoolQueue = new Queue<GameObject>();

        public void PoolObjectDeferred(GameObject go)
        {
            if (go == null) return;
            if(_deferredPoolQueue == null) _deferredPoolQueue = new Queue<GameObject>();
            _deferredPoolQueue.Enqueue(go);
        }

        public void LateUpdate()
        {
            // Pool objects at late update
            while (_deferredPoolQueue.Count > 0)
            {
                var go = _deferredPoolQueue.Dequeue();
                
                if (go != null)
                {
                    PoolObject(go);
                }
            }
        }

        public void PoolObject(GameObject objectToPool)
        {
            if (objectToPool == null) return;
            if (!objectToPool.TryGetComponent(out PoolableComponent poolable))
            {
                Debug.LogWarning($"Prefab {objectToPool.name} doesn't have poolable component");
                UnityEngine.Object.Destroy(objectToPool);
                return;
            }

            GameObject key = poolable.CorrespondingPrefab;
            if (objectToPool == null) return;

            if (!_pool.ContainsKey(key))
            {
                Debug.LogWarning($"Prefab {objectToPool.name} doesn't have a field in the pool");
                UnityEngine.Object.Destroy(objectToPool);
                return;
            }

            if (_pool[key].Count >= _size)
            {
                UnityEngine.Object.Destroy(objectToPool);
            }
            else
            {
                // Sıra önemi çok fark etmiyor ama genelde önce parent’ı alıp sonra SetActive(false) yapmak okunaklıdır
                objectToPool.transform.SetParent(_poolParent, false);
                objectToPool.SetActive(false);

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
                dequeued.transform.SetParent(null);
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
        /// Grows a sub pool by a fixed step when it runs dry.
        ///
        /// This used to grow along the Fibonacci series, which is exactly the wrong shape here: the growth
        /// step keeps rising, so the longer a pool survives the more objects a single miss instantiates, and
        /// it always happens inside a gameplay frame. A profile caught one such miss creating 233 objects at
        /// once. A fixed step bounds the worst case to something a frame can absorb; anything more than that
        /// should be handled by raising the step, not by a growing stall in the middle of the fight.
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
            Grow(key, GrowthStep);
        }

        // Never exceeds the per-sub-pool maximum.
        private void Grow(GameObject key, int count)
        {
            int target = Mathf.Min(_pool[key].Count + count, _size);
            for (int i = _pool[key].Count; i < target; i++)
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
        
    }
}