using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public class GameManager : Singleton<GameManager>
    {
        public PrefabPool Pool;
        public bool GameIsPaused = false;
        
        protected virtual void Awake()
        {
            Pool = new PrefabPool(transform, 1000);
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
        }

        #region pool

        public IEnumerator PoolObjectAfterTime(GameObject objToPool, float delay, UnityAction handler = null)
        {
            IEnumerator coroutine = PoolRoutine(objToPool, delay, handler);
            StartCoroutine(coroutine);
            return coroutine;
        }

        private IEnumerator PoolRoutine(GameObject objToPool, float delay, UnityAction handler)
        {
            yield return new WaitForSeconds(delay);
            Pool.PoolObject(objToPool);
            handler?.Invoke();
        }

        #endregion
    }
}