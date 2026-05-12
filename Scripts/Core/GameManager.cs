using System;
using System.Collections;
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
        public float LoadingScreenCloseDelay = 0f;
        public GameObject LoadingScreen;

        [NonSerialized] public LevelTransitionData LevelTransitionData;
        [NonSerialized] public string PreviousSceneName = "";
        
        //Submanagers
        private SubManager[] _subManagers;
        private SubManager[] _sceneSubManagers;
        private readonly Dictionary<Type, SubManager> _persistentManagersByType = new Dictionary<Type, SubManager>();
        private readonly Dictionary<Type, SubManager> _sceneManagersByType = new Dictionary<Type, SubManager>();

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
            foreach (var subManager in _subManagers)
            {
                _persistentManagersByType[subManager.GetType()] = subManager;
            }
            
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
        
        public SubManager GetSubManagerByType<T>() where T : SubManager
        {
            var type = typeof(T);
            if (_persistentManagersByType.TryGetValue(type, out var manager)) return manager;
            if (_sceneManagersByType.TryGetValue(type, out manager)) return manager;

            // Polymorphic fallback: find a registered manager that is assignable to T
            foreach (var kv in _persistentManagersByType)
                if (type.IsAssignableFrom(kv.Key)) return kv.Value;
            foreach (var kv in _sceneManagersByType)
                if (type.IsAssignableFrom(kv.Key)) return kv.Value;

            return null;
        }

        protected virtual void OnSubmanagersInitialized(SubManager[] subManagers)
        {
            foreach (var subManager in subManagers)
            {
                subManager.OnSubmanagersInitialized();
            }

            StartCoroutine(CloseLoadingScreen());
        }

        private IEnumerator CloseLoadingScreen()
        {
            yield return new WaitForSeconds(LoadingScreenCloseDelay);
            if (LoadingScreen != null) LoadingScreen.SetActive(false);
        }

        public void ToggleSubManager<T>(bool toggle) where T : SubManager
        {
            T subManager = GetSubManagerByType<T>() as T;
            if (subManager == null) return;
            subManager.enabled = toggle;
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
            ctx.PreviousSceneName = GetCurrentSceneName();
            ctx.LevelTransitionData = levelTransitionData;
            //Clear existing scene specific sub managers
            if(ctx._sceneSubManagers != null)
            {
                foreach(var sceneSubManager in ctx._sceneSubManagers)
                {
                    sceneSubManager.Cleanup();
                }
                ctx._sceneSubManagers = null;
                ctx._sceneManagersByType.Clear();
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
            
            _sceneManagersByType.Clear();
            foreach (var subManager in _sceneSubManagers)
            {
                _sceneManagersByType[subManager.GetType()] = subManager;
            }
            
            await InitializeSubManagers(_sceneSubManagers);
            container.ActivateManagerDependentSceneObjects();
            ResumeGame();
            
            //Call On scene entry for every manager. They may need to know
            if(_subManagers != null)
            {
                foreach (var manager in _subManagers)
                {
                    manager.OnSceneEntry();
                }
            }

            if(_sceneSubManagers != null)
            {
                foreach (var manager in _sceneSubManagers)
                {
                    manager.OnSceneEntry();
                }
            }

            // Call OnPostSceneLoaded with the transition data and previous scene name
            LevelTransitionData transitionData = LevelTransitionData;
            string previousScene = PreviousSceneName;
            if(_subManagers != null)
            {
                foreach (var manager in _subManagers)
                {
                    manager.OnPostSceneLoaded(transitionData, previousScene);
                }
            }
            if(_sceneSubManagers != null)
            {
                foreach (var manager in _sceneSubManagers)
                {
                    manager.OnPostSceneLoaded(transitionData, previousScene);
                }
            }
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