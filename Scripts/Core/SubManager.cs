using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core
{
    public class SubManager : MonoBehaviour, ISaveable
    {
        [NonSaveableField] protected GameManager ParentManager;
        [NonSaveableField] protected bool Initialized = false;

        [Header("SubManager")]
        public string DataStorageProviderId;

        public virtual async UniTask Initialize(GameManager gameManager)
        {
            ParentManager = gameManager;
            Initialized = true;
        }

        public virtual void OnSubmanagersInitialized()
        {
        }

        /// <summary>
        /// Called when the GameManager enters a new scene.
        /// </summary>
        public virtual void OnSceneEntry()
        {
        }

        /// <summary>
        /// Called when the GameManager leaves the current scene.
        /// </summary>
        public virtual void OnSceneLeave()
        {
        }

        /// <summary>
        /// Called after all sub-managers are initialized and the scene is fully ready.
        /// Provides the transition data and the name of the previous scene.
        /// </summary>
        public virtual void OnPostSceneLoaded(LevelTransitionData transitionData, string previousScene)
        {

        }

        /// <summary>
        /// Called when a scene-specific SubManager is removed during scene cleanup.
        /// </summary>
        public virtual void Cleanup()
        {
        }

        public static T GetContext<T>() where T : SubManager
        {
            return GameManager.Instance.GetSubManagerByType<T>() as T;
        }

        #region State

        public virtual byte[] Serialize()
        {
            return null;
        }

        public virtual void Deserialize(byte[] data)
        {
        }

        public virtual void LoadState()
        {
            if (!GameStateManager.LoadData(this, DataStorageProviderId))
                SetDefaultState();
        }

        [Button("Save State")]
        public void SaveState()
        {
            GameStateManager.UpdateSaveData(this, DataStorageProviderId);
        }

        public virtual void SetDefaultState()
        {
        }

        [Button("Clear State")]
        public virtual void ClearState()
        {
            GameStateManager.ClearSaveData(this, DataStorageProviderId);
            SetDefaultState();
        }

        #endregion
    }
}
