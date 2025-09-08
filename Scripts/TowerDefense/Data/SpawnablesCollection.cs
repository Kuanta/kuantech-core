using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
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
        [Serializable]
        public class SpawnableEntry
        {
            [Tooltip("List order = SpawnableIndex. Otomatik set edilir.")]
            public int SpawnableIndex;

            [Header("Runtime")]
            public ActorBlueprint ActorBlueprint;

            [Header("Design Meta (Generation)")]
            [Min(1)] public int Cost = 10;
            [Min(0.1f)] public float ConcurrencyWeight = 1f;
            public List<EnemyTagAsset> Tags = new List<EnemyTagAsset>();
        }
        
        public List<SpawnableEntry> Spawnables;

        public ActorBlueprint GetActorTemplate(int index)
        {
            if (Spawnables.IsNullOrEmpty()) return null;
            index = index % Spawnables.Count;
            return Spawnables[index].ActorBlueprint;
        }
        
        public SpawnableEntry GetEntry(int index)
        {
            if (Spawnables == null || index < 0 || index >= Spawnables.Count) return null;
            return Spawnables[index];
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // sıralamaya göre indexleri güncelle
            if (Spawnables == null) return;
            for (int i = 0; i < Spawnables.Count; i++)
                Spawnables[i].SpawnableIndex = i;

            // blueprint’i olmayan entry’leri istersen buradan işaretleyebilirsin
        }

        [ContextMenu("Sort By Cost (Asc)")]
        private void SortByCost()
        {
            Spawnables = Spawnables.OrderBy(e => e.Cost).ToList();
            OnValidate();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}