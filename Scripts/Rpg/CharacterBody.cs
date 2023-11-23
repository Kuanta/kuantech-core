using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Data;
using Kuantech.Rpg.Inventory;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Rpg
{
    [Serializable]
    public struct InplaceEquipment
    {
        public int TemplateId;
        public Enums.EquipmentSlotType EquipmentType;
        public List<GameObject> EquipmentParts;
        public bool ToggleHair;
        public bool ToggleFacialHair;
        public bool ToggleHead;
        public Color PrimaryColor;
        public Color SecondaryColor;

        public void Toggle(bool toggle)
        {
            foreach (var part in EquipmentParts)
            {
                part.SetActive(toggle);
            }
        }
    }
    
    [Serializable]
    public struct EquipmentToBonePair
    {
        public Enums.EquipmentSlotType EquipmentType;
        public Enums.BoneTypes BoneTypes;
    }
    
    [Serializable]
    public struct VisualToBonePair
    {
        public Enums.VisualFields VisualType;
        public Enums.BoneTypes BoneType;
    }
    
    [Serializable]
    public struct BonePair
    {
        public Enums.BoneTypes BoneType;
        public Transform Bone;
    }
    public class CharacterBody : MonoBehaviour
    {
        [Header("Actor")] 
        public RpgActor Actor;
        
        [Header("Materials")] 
        public Renderer BodyRenderer;

        [Header("Bones and Slots")] 
        public Animator Animator;
        public AnimatorModule AnimatorModule;
        public Transform RootBone;
        public Enums.Genders Gender;
        public Enums.Races Race;
        public List<BonePair> ObjectSlots;
        public List<Material> Skins;
        public List<VisualToBonePair> VisualToBonePairs;
        public List<EquipmentToBonePair> EquipmentToBonePairs;

        [Header("Head & Hairs")] 
        public GameObject Head;
        public GameObject Eyebrow; 
        public GameObject Hair;
        public GameObject FacialHair;
        
        [Header("Bones")]
        public Dictionary<string, Transform> Skeleton;
        public Dictionary<Enums.VisualFields, Enums.BoneTypes> VisualsToBoneMapping; //Map visuals to a bone type
        public Dictionary<Enums.BoneTypes, Transform> BoneTable;
        public Dictionary<Enums.VisualFields, int> VisualsTable; //Visual field to index
        public Dictionary<Enums.BoneTypes, GameObject> SlottedObjects;
        public Dictionary<Enums.EquipmentSlotType, Enums.BoneTypes> EquipmentsToBoneMapping;

        [Header("Inplace Equipments")] 
        public List<InplaceEquipment> InplaceEquipmentsList;
        public List<InplaceEquipment> DefaultInplacEquipmentsList;
        public Dictionary<int, InplaceEquipment> InplaceEquipments;
        public Dictionary<Enums.EquipmentSlotType, InplaceEquipment> DefaultInplaceEquipments;

        private bool _initialized = false;

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            BoneTable = new Dictionary<Enums.BoneTypes, Transform>();
            VisualsToBoneMapping = new Dictionary<Enums.VisualFields, Enums.BoneTypes>();
            SlottedObjects = new Dictionary<Enums.BoneTypes, GameObject>();
            EquipmentsToBoneMapping = new Dictionary<Enums.EquipmentSlotType, Enums.BoneTypes>();
            InplaceEquipments = new Dictionary<int, InplaceEquipment>();
            DefaultInplaceEquipments = new Dictionary<Enums.EquipmentSlotType, InplaceEquipment>();
            
            foreach (BonePair pair in ObjectSlots)
            {
                BoneTable[pair.BoneType] = pair.Bone;
                SlottedObjects[pair.BoneType] = null;
            }

            foreach (var pair in VisualToBonePairs)
            {
                VisualsToBoneMapping[pair.VisualType] = pair.BoneType;
            }

            foreach (var pair in EquipmentToBonePairs)
            {
                EquipmentsToBoneMapping[pair.EquipmentType] = pair.BoneTypes;
            }

            foreach (var inplaceEquipment in InplaceEquipmentsList)
            {
                InplaceEquipments[inplaceEquipment.TemplateId] = inplaceEquipment;
            }

            foreach (var defaultEquipment in DefaultInplacEquipmentsList)
            {
                DefaultInplaceEquipments[defaultEquipment.EquipmentType] = defaultEquipment;
                ToggleDefaultInplaceEquipment(defaultEquipment.EquipmentType, true);
            }
            
            Skeleton = new Dictionary<string, Transform>();
  
            //Make sure that a slot is assigned to the root bone
            if (RootBone == null)
            {
                RootBone = transform;
            }
            else
            {
                foreach (Transform bone in RootBone.GetComponentsInChildren<Transform>())
                {
                    Skeleton[bone.name] = bone;
                }
            }

            if (BoneTable.ContainsKey(Enums.BoneTypes.RootBone)) return;
            BoneTable[Enums.BoneTypes.RootBone] = RootBone;
            SlottedObjects[Enums.BoneTypes.RootBone] = null;
        }

        public void SetActor(RpgActor actor)
        {
            if (actor.AnimatorModule != null)
            {
                actor.AnimatorModule.Animator = Animator;
            }
        }
        public void SlotObject(Enums.EquipmentSlotType slotType, GameObject prefab)
        {
            if (!EquipmentsToBoneMapping.ContainsKey(slotType))
            {
                Debug.LogError($"{gameObject.name} has no available bone for {slotType.ToString()}");
                return;
            }
            SlotObject(EquipmentsToBoneMapping[slotType], prefab);
            ToggleDefaultInplaceEquipment(slotType, false);
        }
        
        /// <summary>
        /// Adds a visual object
        /// </summary>
        /// <param name="boneType"></param>
        /// <param name="prefab"></param>
        public void SlotObject(Enums.BoneTypes boneType, GameObject prefab)
        {
            if (!BoneTable.ContainsKey(boneType))
            {
                boneType = Enums.BoneTypes.RootBone;
            }
            RemoveObject(boneType); //Check if there is already an object here

            GameObject instantiated = GameManager.Instance.Pool.GetObject(prefab);
            EquipmentModel model = instantiated.GetComponent<EquipmentModel>();
            Enums.BoneTypes visualBone = boneType; //Equipment is assigned to boneType but will be visualized at visualBone
            if (model != null)
            {
                visualBone = model.CurrentSlot;
            }
            Transform bone = BoneTable[visualBone]; 
            //A weapon (like a bow) can be slotted to main hand but prefer to be parented to the off-hand bone for visuals
            
            SlottedObjects[boneType] = instantiated;
            SkinnedMeshRenderer meshRenderer = instantiated.GetComponent<SkinnedMeshRenderer>();
            
            // If object has a skinned mesh renderer, it should be handled with the bone animation
            // otherwise, it should be placed under the corresponding slot
            if (meshRenderer == null)
            {
                //todo: Reconsider enabling this
                // if (placer == null)
                // {
                //     instantiated.transform.SetParent(bone);
                //     instantiated.transform.localPosition = Vector3.zero;
                //     instantiated.transform.localScale = Vector3.one;
                //     instantiated.transform.localRotation = Quaternion.identity;
                //     return;
                // }
                // placer.Place(boneType, bone);
                //

                instantiated.transform.SetParent(bone);
                instantiated.transform.localPosition = Vector3.zero;
                instantiated.transform.localScale = Vector3.one;
                instantiated.transform.localRotation = Quaternion.identity;
            }
            else
            {
                ChangeBones(meshRenderer);
                meshRenderer.rootBone = bone;
                instantiated.transform.SetParent(transform);
                instantiated.transform.localPosition = Vector3.zero;
                instantiated.transform.localScale = Vector3.one;
                instantiated.transform.localRotation = Quaternion.identity;
            }
            
            //Weapons change animation set
            if (model == null) return;
            model.SwapWeaponAnimations(AnimatorModule.Animator, boneType);

            if (boneType == Enums.BoneTypes.MainHandBone) //todo: This here needs heavy refactoring
            {
                EffectsModule em = (EffectsModule)Actor.GetModuleByType(typeof(EffectsModule));
                if (em != null)
                {
                    em.SetAttackEffects(model.AttackEffects);
                    em.SetAlternativesEffects(model.AlternativeEffects);
                    if(model.ImpactEffectPrefab != null) em.SetImpactEffect(model.ImpactEffectPrefab);
                }
            }
        }
        
        /// <summary>
        /// Slotting function for inplace equipments
        /// </summary>
        /// <param name="tempalteId"></param>
        public void SlotInplaceEquipment(int tempalteId)
        {
            if (!InplaceEquipments.ContainsKey(tempalteId)) return;
            Enums.EquipmentSlotType slotType = InplaceEquipments[tempalteId].EquipmentType;
            ToggleDefaultInplaceEquipment(slotType, false);
            InplaceEquipments[tempalteId].Toggle(true);
            ToggleHeadElements(InplaceEquipments[tempalteId].ToggleHair, InplaceEquipments[tempalteId].ToggleFacialHair, InplaceEquipments[tempalteId].ToggleHead, false);
        }
        
        /// <summary>
        /// Toggles head elements
        /// </summary>
        /// <param name="toggleHair"></param>
        /// <param name="toggleFacialHair"></param>
        /// <param name="toggleHead"></param>
        /// <param name="toggle"></param>
        public void ToggleHeadElements(bool toggleHair, bool toggleFacialHair, bool toggleHead, bool toggle)
        {
            if(toggleHair) ToggleHair(toggle);
            if(toggleFacialHair) ToggleFacialHair(toggle);
            if(toggleHead) ToggleHead(toggle);
        }
        
        public void ToggleHair(bool toggle)
        {
            if(Hair != null) Hair.SetActive(toggle);
        }

        public void ToggleFacialHair(bool toggle)
        {
            if(FacialHair != null) FacialHair.SetActive(toggle);
        }

        public void ToggleHead(bool toggle)
        {
            if(Head != null) Head.SetActive(toggle);
            if(Eyebrow != null) Eyebrow.SetActive(toggle);
        }
        
        public void ToggleDefaultInplaceEquipment(Enums.EquipmentSlotType slotType, bool toggle)
        {
            if (DefaultInplaceEquipments == null || !DefaultInplaceEquipments.ContainsKey(slotType)) return;
            DefaultInplaceEquipments[slotType].Toggle(toggle);

            if (slotType != Enums.EquipmentSlotType.Head) return;
            if (DefaultInplaceEquipments[slotType].ToggleHair)
            {
                ToggleHair(!toggle);
            }
            else
            {
                ToggleHair(true);
            }

            if (DefaultInplaceEquipments[slotType].ToggleFacialHair)
            {
                ToggleFacialHair(!toggle);
            }
            else
            {
                ToggleFacialHair(true);
            }
            
            if (DefaultInplaceEquipments[slotType].ToggleHead)
            {
                ToggleHead(!toggle);
            }
            else
            {
                ToggleHead(true);
            }
        }
        
        public void ToggleAllDefaultInplaceEquipments(bool toggle)
        {
            if (DefaultInplaceEquipments == null) return;
            foreach (var slotType in DefaultInplaceEquipments.Keys)
            {
                ToggleDefaultInplaceEquipment(slotType, toggle);
            }
        }
        
        public void RemoveObject(Enums.EquipmentSlotType slotType)
        {
            if (!EquipmentsToBoneMapping.ContainsKey(slotType))
            {
                Debug.LogError($"{gameObject.name} has no available bone for {slotType.ToString()}");
                return;
            }
            RemoveObject(EquipmentsToBoneMapping[slotType]);
        }
        public void RemoveObject(Enums.BoneTypes boneType)
        {
            if (!SlottedObjects.ContainsKey(boneType) || SlottedObjects[boneType] is null) return;
            GameManager.Instance.Pool.PoolObject(SlottedObjects[boneType]);
            SlottedObjects[boneType] = null;
            
            if (boneType == Enums.BoneTypes.MainHandBone) //todo: This here needs heavy refactoring
            {
                EffectsModule em = (EffectsModule)Actor.GetModuleByType(typeof(EffectsModule));
                if (em != null)
                {
                    em.RemoveCurrentAttackEffects();
                    em.RemoveCurrentAlternativeAttackEffects();
                    em.RemoveImpactEffect();
                }
            }
        }

        public void RemoveInplaceObject(int id)
        {
            if (!InplaceEquipments.ContainsKey(id)) return;
            InplaceEquipments[id].Toggle(false);
            //Check hairs
            if (InplaceEquipments[id].ToggleHair)
            {
                ToggleHair(true);
            }

            if (InplaceEquipments[id].ToggleFacialHair)
            {
                ToggleFacialHair(true);
            }

            if (InplaceEquipments[id].ToggleHead)
            {
                ToggleHead(true);
            }
        }
        
        public void ChangeVisual(Enums.VisualFields VisualType, int value)
        {
            Initialize(); //In case VisualsToBoneMapping is null
            if (!VisualsToBoneMapping.ContainsKey(VisualType)) return;
            Enums.BoneTypes boneType = VisualsToBoneMapping[VisualType];
            GameObject prefab = VisualizationManager.Instance.GetVisualObject(Race, Gender, VisualType, value);
            if (prefab == null) return;
            SlotObject(boneType, prefab);            
        }
        public void ChangeSkin(int value)
        {
            if (BodyRenderer is null || Skins is null || Skins.Count == 0) return;
            value = value % Skins.Count;
            BodyRenderer.material = Skins[value];
        }

        private void ChangeBones(SkinnedMeshRenderer renderer)
        {
            if (renderer.bones == null) return;
            Transform[] newBones = new Transform[renderer.bones.Length];
            for (int i = 0; i < renderer.bones.Length; i++)
            {
                if(!Skeleton.ContainsKey(renderer.bones[i].name)) continue;
                newBones[i] = Skeleton[renderer.bones[i].name];
            }
            renderer.bones = newBones;
        }
        
        
        public void Clear()
        {
            try
            {
                if (SlottedObjects == null) return;
                foreach (var key in SlottedObjects.Keys)
                {
                    if (SlottedObjects[key] == null) return;
                    RemoveObject(key);
                }
                SlottedObjects.Clear();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #region Editor

        [Button("Toggle Inplace")]
        public void ToggleInplaceEditor(int id, bool toggle)
        {
            foreach (var equipment in InplaceEquipmentsList)
            {
                if (equipment.TemplateId == id)
                {
                    foreach (var defaultEquipment in DefaultInplacEquipmentsList)
                    {
                        if (defaultEquipment.EquipmentType == equipment.EquipmentType)
                        {
                            defaultEquipment.Toggle(!toggle);
                        }
                    }
                    equipment.Toggle(toggle);
                    if (equipment.ToggleHair)
                    {
                        ToggleHair(!toggle);
                    }

                    if (equipment.ToggleFacialHair)
                    {
                        ToggleFacialHair(!toggle);
                    }

                    if (equipment.ToggleHead)
                    {
                        ToggleHead(!toggle);
                    }
                    return;
                }
            }
        }
        #endregion
    }
}