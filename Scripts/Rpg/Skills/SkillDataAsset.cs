using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    [CreateAssetMenu(fileName = "SkillDataAsset", menuName = "Kuantech/Rpg/Skills/SkillDataAsset")]
    public class SkillDataAsset : ScriptableObject
    {
        public string SkillId;
        public string SkillName;
        public string SkillDescription;
        public List<SkillBehaviourData> SkillBehaviours;
        public List<SkillVariableData> SkillVariableDatas;
        public float SkillCooldown;
    }
}