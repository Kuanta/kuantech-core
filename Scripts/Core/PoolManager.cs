using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public class PoolManager : SubManager
    {
        public PrefabPool Pool;

        public override async UniTask Initialize(GameManager gameManager)
        {
            Pool = new PrefabPool(transform, 1000);
            await base.Initialize(gameManager);
        }
        
        public static GameObject GetObjectFromPool(GameObject prefab)
        {
            PoolManager pm = GetContext<PoolManager>();

            if (pm == null)
            {
                Debug.LogWarning("Requested object from pool while Pool Manager doesn't exist");
                return Object.Instantiate(prefab);
            }
            return pm.Pool.GetObject(prefab);
        }
        public static void PoolObject(GameObject objectToPool, float delay=0)
        {
            PoolManager pm = GetContext<PoolManager>();
            if (pm == null)
            {
                Destroy(objectToPool);
                return;
            }
            pm.PoolObjectAfterTime(objectToPool, delay);
        }
        public IEnumerator PoolObjectAfterTime(GameObject objToPool, float delay, UnityAction handler = null)
        {
            PoolManager pm = GetContext<PoolManager>();
            IEnumerator coroutine = pm.PoolRoutine(objToPool, delay, handler);
            StartCoroutine(coroutine);
            return coroutine;
        }
       
        private IEnumerator PoolRoutine(GameObject objToPool, float delay, UnityAction handler)
        {
            yield return new WaitForSeconds(delay);
            Pool.PoolObject(objToPool);
            handler?.Invoke();
        }

    }
}