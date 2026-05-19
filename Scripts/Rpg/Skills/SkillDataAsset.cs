using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    [CreateAssetMenu(fileName = "SkillDataAsset", menuName = "Kuantech/Rpg/Skills/SkillDataAsset")]
    public class SkillDataAsset : ScriptableObject
    {
        public enum SkillCastTypes
        {
            None,
            Targeted,
            Self,
            Directional,
            ToPoint,
        }
        
        [Header("Skill Info")]
        public string SkillId;
        [Tooltip("Leave empty for skills that have no rank variants.")]
        public string BaseSkillId;
        public int Rank = 0;
        public string SkillName;
        public string SkillDescription;
        public SkillCastTypes SkillCastType;
        public bool WaitRotationalAlignToTarget;

        [Header("Required Resource")]
        public ResourceAsset RequiredResource;
        public float RequiredResourceAmount;
        
        [Header("Behaviours")]
        public List<SkillBehaviourData> SkillBehaviours;
        
        [SerializeReference]
        public SkillCastChecker SkillCastChecker;
        
        [Header("Skill Variables")]
        public List<SkillVariableData> SkillVariableDatas;
        public float SkillCooldown;
        [Tooltip("Random seconds added to cooldown after each cast. Prevents groups of enemies casting in sync.")]
        public float CooldownJitter = 0f;
        public float SkillRange;
        
        //todo: This is ai related, remove it from here
        [Header("Targeting Behaviour")] 
        public bool TargetsAllies = false;
        public bool TargetsEnemies = true;
        public TargetPriorityBehaviour TargetPriorityBehaviour;
    }
}