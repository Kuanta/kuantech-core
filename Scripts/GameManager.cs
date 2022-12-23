using UnityEngine;

namespace Kuantech.Core
{
    public class GameManager : Singleton<GameManager>
    {
        public PrefabPool Pool;
        public bool GameIsPaused = false;
        
        protected void Awake()
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
    }
}