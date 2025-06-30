using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// A draggable class used for units that are spawned with drag
    /// </summary>
    public class SpawnerDraggable : MonoBehaviour
    {
        [SerializeField] private Draggable Draggable;
        [SerializeField] private MonoBehaviour spawnableBehaviour; // Editor'de atanabilir
        [SerializeField] private bool RegisterToCurrentLevel;

        private ISpawnable Spawnable => spawnableBehaviour as ISpawnable;
        
        private void Start()
        {
            if (Draggable == null) return;
            Draggable.OnSuccesfullDropEvent += OnSuccesfullDropAsProxyDraggable;
        }
        
        public void OnSuccesfullDropAsProxyDraggable(DropInformation dropInformation)
        {
            Spawnable.Spawn();
            if (RegisterToCurrentLevel)
            {
                //Register to level
                Level level = LevelManager.GetCurrentLevel();
                if (level == null) return;
                level.AddSpawnable(Spawnable);
            }
        }
    }
}