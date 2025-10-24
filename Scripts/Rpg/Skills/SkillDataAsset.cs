using System.Collections.Generic;
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
        public string SkillName;
        public string SkillDescription;
        public SkillCastTypes SkillCastType;
        
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
        public float SkillRange;
    }
}