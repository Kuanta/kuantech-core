using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    [CreateAssetMenu(fileName = "PassiveSkillDataAsset", menuName = "Kuantech/Rpg/Skills/Passive Skill")]
    public class PassiveSkillDataAsset : ScriptableObject
    {
        [Header("Info")]
        public string SkillId;
        [Tooltip("Leave empty for skills that have no rank variants.")]
        public string BaseSkillId;
        public int Rank = 0;
        public string SkillName;
        public string SkillDescription;

        [Header("Proc")]
        [Range(0f, 1f)]
        [Tooltip("1 = always proc, 0 = never. Checked on each TryProc() call.")]
        public float ProcChance = 1f;
        [Tooltip("Minimum seconds between procs. 0 = no limit.")]
        public float ProcCooldown = 0f;

        [Header("Effects")]
        [SerializeReference]
        public List<PassiveEffect> Effects = new();

        [Header("Variables")]
        public List<SkillVariableData> SkillVariableDatas = new();
    }
}
