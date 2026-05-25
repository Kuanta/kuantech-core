using System.Collections.Generic;
using Kuantech.Inventory;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// A visual piece that lives on the actor prefab — defaults (hair, beard, naked body)
    /// and in-place equipment meshes (Synty-style pre-placed armor).
    /// Extends ItemVisual so the inventory system can treat it as any other item visual.
    /// </summary>
    public class ActorVisualPart : ItemVisual
    {
        public bool IsDefault;
        public List<GameObject> ConnectedObjects;

        public void Toggle(bool active)
        {
            gameObject.SetActive(active);
            if (ConnectedObjects == null) return;
            foreach (var obj in ConnectedObjects)
                if (obj != null) obj.SetActive(active);
        }
    }
}
