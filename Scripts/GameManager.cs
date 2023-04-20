using System.Collections;
using UnityEngine;

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

        public void PoolObjectAfterTime(GameObject objToPool, float delay)
        {
            StartCoroutine(PoolRoutine(objToPool, delay));
        }

        private IEnumerator PoolRoutine(GameObject objToPool, float delay)
        {
            yield return new WaitForSeconds(delay);
            Pool.PoolObject(objToPool);
        }

        #endregion
    }
}