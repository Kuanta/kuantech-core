using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Kuantech.Core
{
    public class GameManager : Singleton<GameManager>
    {
        public bool GameIsPaused = false;
        protected bool SubManagersInitialized = false;

        [Header("Loading Screen")]
        public GameObject LoadingScreen;

        //Submanagers
        private SubManager[] _subManagers;
        private SubManager[] _sceneSubManagers;

        protected virtual void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (LoadingScreen != null)
            {
                LoadingScreen.gameObject.SetActive(true);
            }
        }

        protected virtual void Start()
        {
            StartGame();
        }

        protected async void StartGame()
        {
            //Get and initialize persistent submanagers
            _subManagers = GetComponentsInChildren<SubManager>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            await Initialize();
            OnNewScene();
        }
        protected virtual async UniTask Initialize()
        {
            await InitializeSubManagers(_subManagers);
        }

        #region Game Lifecycle

        public static bool IsGamePaused()
        {
            if (GameManager.Instance == null) return false;
            return GameManager.Instance.GameIsPaused;
        }
        public void PauseGame()
        {
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
        }

        #endregion
        
        #region SubManagers

        private async UniTask InitializeSubManagers(SubManager[] subManagers)
        {
            List<UniTask> tasks = new List<UniTask>();
            foreach (SubManager subManager in subManagers)
            {
                tasks.Add(subManager.Initialize(this));
            }

            SubManagersInitialized = false;
            await UniTask.WhenAll(tasks.ToArray());

            OnSubmanagersInitialized(subManagers);
            SubManagersInitialized = true;
        }
        
        public SubManager GetSubManagerByType<T>()
        {
            if(_subManagers == null) return null;

            for (int i = 0; i < _subManagers.Length; i++)
            {
                if (_subManagers[i] is T)
                {
                    return _subManagers[i];
                }
            }
            if(_sceneSubManagers != null)
            {
                for (int i = 0; i < _sceneSubManagers.Length; i++)
                {
                    if (_sceneSubManagers[i] is T)
                    {
                        return _sceneSubManagers[i];
                    }
                }
            }
            return null; // Return null if no matching submanager is found
        }

        protected virtual void OnSubmanagersInitialized(SubManager[] subManagers)
        {
            foreach (var subManager in subManagers)
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
        
        #region SceneManagement
        public void ChangeScene(string sceneName)
        {
            //Clear existing scene specific sub managers
            if(_sceneSubManagers != null)
            {
                foreach(var sceneSubManager in _sceneSubManagers)
                {
                    sceneSubManager.OnSceneLeave();
                }
                _sceneSubManagers = null;
            }
            
            //Change scene
            SceneManager.LoadScene(sceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            OnNewScene();
        }

        private async void OnNewScene()
        {
            //Check new scene submanagers
            SceneSubManagerContainer container = FindObjectOfType<SceneSubManagerContainer>();
            if(container == null) return;
            _sceneSubManagers = container.GetSubManagers();
            await InitializeSubManagers(_sceneSubManagers);
            container.ActivateManagerDependentSceneObjects();
        }
        #endregion
    }
}