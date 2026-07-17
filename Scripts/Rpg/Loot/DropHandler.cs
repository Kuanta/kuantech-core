using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kuantech.Rpg
{
    [Serializable]
    public struct DropEntry
    {
        public DropObject Prefab;
        [Range(0f, 1f)] public float DropChance;
        public int MinCount;
        public int MaxCount;
    }

    public class DropHandler : ActorModule
    {
        [Header("Drops")]
        public List<DropEntry> Drops;
        public float HorizontalOffsetDist = 0.5f;
        public float SpawnDelay = 0.06f;

        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if (newState == ActorState.Dead)
                StartCoroutine(SpawnDropsCoroutine());
        }

        private IEnumerator SpawnDropsCoroutine()
        {
            if (Drops == null) yield break;
            foreach (var entry in Drops)
            {
                if (entry.Prefab == null) continue;
                if (Random.value > entry.DropChance) continue;
                int count = Random.Range(entry.MinCount, Mathf.Max(entry.MinCount, entry.MaxCount) + 1);
                for (int i = 0; i < count; i++)
                {
                    SpawnDrop(entry.Prefab);
                    yield return new WaitForSeconds(SpawnDelay);
                }
            }
        }

        private void SpawnDrop(DropObject prefab)
        {
            if (!PoolManager.GetObjectFromPool(prefab.gameObject).TryGetComponent(out DropObject obj)) return;
            Vector2 offset = Random.insideUnitCircle * HorizontalOffsetDist;
            obj.transform.position = transform.position + new Vector3(offset.x, 0f, offset.y);
            obj.Scatter();
        }
    }
}
