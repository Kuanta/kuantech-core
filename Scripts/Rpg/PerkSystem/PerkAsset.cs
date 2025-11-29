using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;
using ColorUtility = UnityEngine.ColorUtility;

namespace Kuantech.Rpg
{
    [CreateAssetMenu(fileName = "Perk", menuName="Kuantech/Rpg/Perk")]
    public class PerkAsset : MetadataAsset
    {
        [Header("Perk class")]
        [SerializeField] private string PerkClassName;
        [SerializeReference] public PerkConfig PerkConfig;
        
        [Tooltip("For description building")]
        public List<PerkVariable> PerkVariables;
        public int MaxRank = 5;
        public PerkVariable GetPerkVariable(string variableName)
        {
            return PerkVariables.Find(v => v.Name == variableName);
        }
        
        public string BuildDescription(int rank)
        {
            string descriptionTemplate = GetDescription();
            var rx = new Regex(@"\{([A-Za-z_][A-Za-z0-9_]*)\s*(?::([^}]+))?\}", RegexOptions.Compiled);
            string result = rx.Replace(descriptionTemplate, m =>
            {
                string varName = m.Groups[1].Value;
                string fmt     = m.Groups[2].Success ? m.Groups[2].Value : null;

                PerkVariable variable = GetPerkVariable(varName);
                if (variable == null)

                {
                    return "";
                }

                float value = variable.GetDisplayValue(rank);

                string valueString = variable.IsPercentage ? ((value * 100).Stringfy()) + '%' : value.Stringfy();
                return "<color=#" + ColorUtility.ToHtmlStringRGBA(variable.TextColor) + ">" + valueString + "</color>";
            
            });
            return result;
        }

        public Perk CreatePerk()
        {
            if (string.IsNullOrEmpty(PerkClassName))
            {
                return null;
            }

            string fullClassName = PerkClassName;

            Type perkType = Type.GetType(fullClassName);

            if (perkType == null)
            {
                Debug.LogError($"PerkAsset ({name}): '{fullClassName}' adında bir sınıf bulunamadı! Yazım hatasını kontrol et.");
                return null;
            }

            Perk instance = (Perk)Activator.CreateInstance(perkType);

            instance.Initialize(this); 

            return instance;
        }
    }
}