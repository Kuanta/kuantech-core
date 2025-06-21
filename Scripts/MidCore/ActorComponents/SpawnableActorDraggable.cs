using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// A draggable class used for units that are spawned with drag
    /// </summary>
    public class SpawnableActorDraggable : MonoBehaviour
    {
        [SerializeField] private Draggable Draggable;
        [SerializeField] private Actor Actor;

        private void Start()
        {
            if (Draggable == null) return;
            Draggable.OnSuccesfullDropEvent += OnSuccesfullDropAsProxyDraggable;
        }
        
        public void OnSuccesfullDropAsProxyDraggable(DropInformation dropInformation)
        {
            if (Actor == null)
            {
                Actor = GetComponent<Actor>();
                if (Actor == null) return;
            }

            Actor.Spawn();
            
            //Register to level
            Level level = LevelManager.GetCurrentLevel();
            if (level == null) return;
            level.AddSpawnable(Actor);
        }
    }
}