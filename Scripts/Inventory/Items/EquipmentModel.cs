using System;
using System.Collections.Generic;
using Kuantech.Core.Animation;
using Kuantech.Core.FX;
using Kuantech.Data;
using Kuantech.Utils;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Inventory.Items
{
    [Serializable]
    public struct EquipmentPlace
    {
        public TransformStruct Placement;
        public Enums.BoneTypes BoneType;
    }
    public class EquipmentModel : MonoBehaviour
    {
        public Enums.BoneTypes CurrentSlot;
        public AnimatorOverrideController AnimationSet;
        public List<EquipmentPlace> PlacementsList;
        public Dictionary<Enums.BoneTypes, TransformStruct> Placements = new Dictionary<Enums.BoneTypes, TransformStruct>();

        [Header("Effects")] 
        public List<EffectTypes> AttackEffects = new List<EffectTypes>();
        public List<float> AttackEffectsDelays = new List<float>();
        public Effect ImpactEffectPrefab; //Should be an emitted effect

        [Header("Head Armors")] 
        [Tooltip("When equipped, this item will disable hair")] public bool DisableHair = false;
        [Tooltip("When equipped, this item will disable facial hair")] public bool DisableFacialHair = false;
        
        public void Awake()
        {
            InitializeMapping();
        }

        public void Place(Enums.BoneTypes slotType, Transform parent = null)
        {
            Vector3 localScale = Vector3.one;
            if (parent != null)
            {
                transform.SetParent(parent);
                localScale = parent.transform.lossyScale;
            }

            if (!Placements.ContainsKey(slotType)) return;
            
            transform.localPosition = Placements[slotType].Position;
            transform.localRotation = Quaternion.Euler(Placements[slotType].Rotation);
            transform.localScale = Vector3.one;
        }

        public void SwapWeaponAnimations(Animator animator, Enums.BoneTypes boneType)
        {
            if (animator == null || AnimationSet == null) return;
            animator.runtimeAnimatorController = AnimationSet;
        }

        public void SavePlacement(Enums.BoneTypes slotType)
        {
            for (int i = 0; i < PlacementsList.Count; i++)
            {
                if (PlacementsList[i].BoneType != slotType) continue;
                PlacementsList[i].Placement.Position = transform.localPosition;
                PlacementsList[i].Placement.Rotation = transform.localEulerAngles;
                return;
            }
            
            TransformStruct transformStruct = new TransformStruct()
            {
                Position = transform.localPosition,
                Rotation = transform.localEulerAngles,
            };
            PlacementsList.Add(new EquipmentPlace
            {
                Placement = transformStruct,
                BoneType = slotType,
            });
            Placements[slotType] = transformStruct;
            InitializeMapping();
            EditorUtility.SetDirty(this);
        }

        public TransformStruct GetPlacement(Enums.BoneTypes slotType)
        {
            if (Placements.ContainsKey(slotType))
            {
                return Placements[slotType];
            }
            else
            {
                return new TransformStruct();
            }
        }

        public void InitializeMapping()
        {
            Placements ??= new Dictionary<Enums.BoneTypes, TransformStruct>();
            Placements.Clear();
            
            foreach (var placement in PlacementsList)
            {
                Placements[placement.BoneType] = placement.Placement;
            }
        }
    }
}