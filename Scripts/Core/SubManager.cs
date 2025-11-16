using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core
{
    public class SubManager : MonoBehaviour, ISaveable
    {
        protected GameManager ParentManager;
        protected bool Initialized = false;
        [Header("SubManager")]
        public bool LoadAfterInitialize = false;
        
        public virtual async UniTask Initialize(GameManager gameManager)
        {
            //Subscribe to events
            ParentManager = gameManager;
            Initialized = true;
        }

        public virtual void OnSubmanagersInitialized()
        {
            if(LoadAfterInitialize) LoadState();
        }

        /// <summary>
        /// Called for scene specific submanagers, when the gamemanager enters a new scene
        /// </summary>
        public virtual void OnSceneEntry()
        {

        }

        /// <summary>
        /// Called for scene specific submanagers, when the gamemanager leaves current scene.
        /// </summary>
        public virtual void OnSceneLeave()
        {
            
        }

        /// <summary>
        /// Called for submanagers that are remvoed during a scene cleanup
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

        public void Deserialize(byte[] data)
        {
            
        }
        
        public virtual void LoadState()
        {
            if (this is GameStateManager) return;
            if (!GameStateManager.LoadData(this))
            {
                SetDefaultState();
            }
        }

        [Button("Save State")]
        public void SaveState()
        {
            if (this is GameStateManager) return; //Gamestate manager shouldn't save itself
            GameStateManager.UpdateSaveData(this);
        }
        
        public virtual void SetDefaultState()
        {
        }
        
        [Button("Clear State")]
        public virtual void ClearState()
        {
            if (this is GameStateManager) return; 
            GameStateManager.ClearSaveData(this);
            SetDefaultState();
        }
        #endregion

    }
}