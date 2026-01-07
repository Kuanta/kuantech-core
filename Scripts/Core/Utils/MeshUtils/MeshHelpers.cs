using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Utils
{
    public static class MeshHelpers
    {

        public static void SetSkinnedMeshBones(SkinnedMeshRenderer skinnedMeshRenderer, Transform rootActorTransform, string rootBoneName = "Hips")
        {
            if (skinnedMeshRenderer == null || rootActorTransform == null) return;

            Transform[] actorBones = rootActorTransform.GetComponentsInChildren<Transform>();
            Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>(actorBones.Length);

            foreach (var bone in actorBones)
            {
                if (!boneMap.ContainsKey(bone.name))
                {
                    boneMap.Add(bone.name, bone);
                }
            }

            Transform[] originalBones = skinnedMeshRenderer.bones;
            Transform[] newBones = new Transform[originalBones.Length];

            for (int i = 0; i < originalBones.Length; i++)
            {
                string boneName = originalBones[i].name;

                if (boneMap.TryGetValue(boneName, out Transform foundBone))
                {
                    newBones[i] = foundBone;
                }
                else
                {
                    newBones[i] = rootActorTransform;
                    
                    #if UNITY_EDITOR
                    Debug.LogWarning($"[MeshHelpers] Kemik bulunamadı: '{boneName}'. Root objeye bağlandı.");
                    #endif
                }
            }

            skinnedMeshRenderer.bones = newBones;


            if (boneMap.TryGetValue(rootBoneName, out Transform newRootBone))
            {
                skinnedMeshRenderer.rootBone = newRootBone;
            }
            else
            {
                skinnedMeshRenderer.rootBone = rootActorTransform;
            }
            
            skinnedMeshRenderer.updateWhenOffscreen = true; 
        }


        public static void CleanupUnusedArmature(Transform armorRoot)
        {
            foreach (Transform child in armorRoot)
            {
                if (child.GetComponent<Renderer>() == null && child.childCount > 0)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }
}