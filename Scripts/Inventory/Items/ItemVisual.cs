using System;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Inventory
{
    public class ItemVisual : MonoBehaviour
    {
        public string TemplateId;
        public VisualSlotType VisualSlot;
        public List<VisualSlotType> SlotsToMask;

        [Header("Skinned Mesh")]
        public SkinnedMeshRenderer[] SkinnedRenderers;

        public bool IsSkinned => SkinnedRenderers != null && SkinnedRenderers.Length > 0;

        //Runtime
        [NonSerialized] public ItemData ItemData;
        [NonSerialized] public bool IsInPlace;
        [NonSerialized] public VisualSlotType RuntimeVisualSlot;

        public virtual void Spawn(ItemData parentItemData)
        {
            ItemData = parentItemData;
        }

        public void Despawn()
        {
            if (IsInPlace)
                gameObject.SetActive(false);
            else
                PoolManager.PoolObject(gameObject);
        }

        public void RebindBones(Transform[] targetBones)
        {
            if (SkinnedRenderers == null) return;
            var boneMap = new Dictionary<string, Transform>(targetBones.Length);
            foreach (var bone in targetBones)
                if (bone != null) boneMap[bone.name] = bone;

            foreach (var smr in SkinnedRenderers)
            {
                if (smr == null) continue;
                var newBones = new Transform[smr.bones.Length];
                for (int i = 0; i < smr.bones.Length; i++)
                    boneMap.TryGetValue(smr.bones[i].name, out newBones[i]);
                smr.bones = newBones;
                if (smr.rootBone != null && boneMap.TryGetValue(smr.rootBone.name, out var newRoot))
                    smr.rootBone = newRoot;
            }
        }
    }
}