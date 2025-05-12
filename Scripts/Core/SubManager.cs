using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Core
{
    public class SubManager : MonoBehaviour, ISaveable
    {
        protected GameManager ParentManager;
        
        public virtual async UniTask Initialize(GameManager gameManager)
        {
            //Subscribe to events
            ParentManager = gameManager;
        }

        public virtual void OnSubmanagersInitialized()
        {
            
        }

        /// <summary>
        /// Called for scene specific submanagers, when the gamemanager enters a new scene
        /// </summary>
        public void OnSceneEntry()
        {

        }

        /// <summary>
        /// Called for scene specific submanagers, when the gamemanager leaves current scene.
        /// </summary>
        public void OnSceneLeave()
        {
            Cleanup();
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

        public void LoadState()
        {
            if (this is GameStateManager) return;
            GameStateManager.LoadData(this);
        }

        public void SaveState()
        {
            if (this is GameStateManager) return; //Gamestate manager shouldn't save itself
            GameStateManager.UpdateSaveData(this);
        }
        #endregion

    }
}