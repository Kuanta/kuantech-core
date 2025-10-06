using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kuantech.Core
{
    /// <summary>
    /// A data to collect all the data needed to transition between levels.
    /// </summary>
    public abstract class LevelTransitionData
    {
        
    }
    
    public class GameManager : Singleton<GameManager>
    {
        public bool GameIsPaused = false;
        protected bool SubManagersInitialized = false;

        [Header("Loading Screen")]
        public GameObject LoadingScreen;

        [NonSerialized] public LevelTransitionData LevelTransitionData;
        
        //Submanagers
        private SubManager[] _subManagers;
        private SubManager[] _sceneSubManagers;

        private bool _startedGame = false;
        protected virtual void Awake()
        {
#if DEV_BUILD
            Debug.Log("DEVELOPMENT BUILD");
#endif
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
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
            if (_startedGame) return;
            _startedGame = true;
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
        public static void PauseGame()
        {
            Time.timeScale = 0f;
           Instance.GameIsPaused = true;
        }

        public static void ResumeGame()
        {
            Time.timeScale = 1f;
            Instance.GameIsPaused = false;
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
        
        /// <summary>
        /// Gets the name of the current scene
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
        
        public static void ChangeScene(string sceneName, LevelTransitionData levelTransitionData = null)
        {
            var ctx = GameManager.Instance;
            ctx.LevelTransitionData = levelTransitionData;
            //Clear existing scene specific sub managers
            if(ctx._sceneSubManagers != null)
            {
                foreach(var sceneSubManager in ctx._sceneSubManagers)
                {
                    sceneSubManager.Cleanup();
                }
                ctx._sceneSubManagers = null;
            }
            
            //Call scene leave for global managers
            foreach (var manager in ctx._subManagers)
            {
                manager.OnSceneLeave();
            }
            if(ctx.LoadingScreen != null) ctx.LoadingScreen.SetActive(true);
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
            ResumeGame();
        }
        
        /// <summary>
        /// Gets the level transition data
        /// </summary>
        /// <returns></returns>
        public static LevelTransitionData GetLevelTransitionData()
        {
            return GameManager.Instance.LevelTransitionData;
        }
        #endregion
    }
}