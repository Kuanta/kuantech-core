using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core.HyperCasual;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public class GameManager : Singleton<GameManager>
    {
        public PrefabPool Pool;
        public bool GameIsPaused = false;
        public Camera MainCamera;
        protected bool SubManagersInitialized = false;

        [Header("Loading Screen")]
        public GameObject LoadingScreen;

        //Submanagers
        private SubManager[] _subManagers;

        protected virtual void Awake()
        {
            Pool = new PrefabPool(transform, 1000);
        }

        protected virtual async void Start()
        {
            await Initialize();
        }

        protected virtual async UniTask Initialize()
        {
            await InitializeSubManagers();

        }
        public void PauseGame()
        {
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
        }
        
        #region SubManagers

        private async UniTask InitializeSubManagers()
        {
            //Initialize SubManagers
            _subManagers = GetComponentsInChildren<SubManager>();
            List<UniTask> tasks = new List<UniTask>();
            foreach (SubManager subManager in _subManagers)
            {
                tasks.Add(subManager.Initialize(this));
            }

            SubManagersInitialized = false;
            await UniTask.WhenAll(tasks.ToArray());
            
            OnSubmanagersInitialized();
            SubManagersInitialized = true;
        }
        
        public SubManager GetSubManagerByType<T>()
        {
            for (int i = 0; i < _subManagers.Length; i++)
            {
                if (_subManagers[i] is T)
                {
                    return _subManagers[i];
                }
            }

            return null; // Return null if no matching submanager is found
        }

        protected virtual void OnSubmanagersInitialized()
        {
            foreach (var subManager in _subManagers)
            {
                subManager.OnSubmanagersInitialized();
            }
            if (LoadingScreen != null) LoadingScreen.SetActive(false);
        }

        public void ToggleSubManager<T>(bool toggle)
        {
            GetSubManagerByType<T>().enabled = toggle;
        }
        #endregion
        
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