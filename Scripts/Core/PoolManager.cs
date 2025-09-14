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
            GameObject gameObj = null;

            if (pm == null)
            {
                Debug.LogWarning("Requested object from pool while Pool Manager doesn't exist");
                gameObj = Object.Instantiate(prefab);
            }
            else
            {
                gameObj = pm.Pool.GetObject(prefab);
            }

            if (gameObj == null) return null;
            gameObj.transform.SetParent(null);
            return gameObj;
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
        
        public void PoolObjectAfterTime(GameObject objToPool, float delay, UnityAction handler = null)
        {
            
            PoolManager pm = GetContext<PoolManager>();
            if (delay <= 0)
            {
                _PoolObject(objToPool, handler);
                return;
            }
            IEnumerator coroutine = pm.PoolRoutine(objToPool, delay, handler);
            StartCoroutine(coroutine);
        }
       
        private IEnumerator PoolRoutine(GameObject objToPool, float delay, UnityAction handler)
        {
            yield return new WaitForSeconds(delay);
            _PoolObject(objToPool, handler);
        }

        private void _PoolObject(GameObject objToPool, UnityAction handler)
        {
            Pool.PoolObject(objToPool);
            handler?.Invoke();
        }
        
        private void LateUpdate()
        {
            if (Pool == null) return;
            Pool.LateUpdate();
        }
    }
}