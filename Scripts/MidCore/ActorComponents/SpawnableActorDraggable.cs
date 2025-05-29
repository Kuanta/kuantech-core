using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// A draggable class used for units that are spawned with drag
    /// </summary>
    public class SpawnableActorDraggable : Draggable
    {
        [SerializeField] private Actor Actor;
        public override void OnSuccesfullDropAsProxyDraggable()
        {
            if (Actor == null)
            {
                Actor = GetComponent<Actor>();
                if (Actor == null) return;
            }

            Actor.Spawn();
        }
    }
}