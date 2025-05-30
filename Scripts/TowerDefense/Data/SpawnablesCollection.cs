using System.Collections.Generic;
using Kuantech.Midcore;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    /// <summary>
    /// A collection of spawnable actors that can be used in games where actors are spawned.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnablesCollection", menuName = "Kuantech/TowerDefense/SpawnablesCollection", order = 1)]
    public class SpawnablesCollection : ScriptableObject
    {
        public List<ActorTemplateAsset> ActorTemplates;

        public ActorTemplateAsset GetActorTemplate(int index)
        {
            if (ActorTemplates.IsNullOrEmpty()) return null;
            index = index % ActorTemplates.Count;
            return ActorTemplates[index];
        }
    }
}