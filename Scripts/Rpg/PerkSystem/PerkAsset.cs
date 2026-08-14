using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Kuantech.Core;
using Kuantech.Rpg.Skills;
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
        
        [Tooltip("For description building. Same type skills use, so a value can live in either place.")]
        public List<SkillVariableData> PerkVariables;
        public int MaxRank = 5;
        [Tooltip("Relative weight for the level-up perk pool's weighted draw. Lives here (not per-pool-" +
                 "entry) so a perk's odds are authored once and stay the same wherever it's offered from " +
                 "(common pool, weapon pool, utility belt).")]
        public float PerkAppearChance = 1f;

        [Header("Perk Dependencies")]
        [Tooltip("Perk required to get this perk")]
        public PerkAsset DependentPerk; 
        [Tooltip("Perks that depends on this")]
        public List<PerkAsset> SubPerks; //Perks that depend on this

        /// <summary>
        /// Finds a variable by id: <paramref name="externalOverride"/> first (if it matches), then the
        /// perk's own list, then whatever the config can resolve (a skill-granting perk forwards to the
        /// granted skill, so those numbers are never duplicated here).
        ///
        /// The override lets a caller substitute one variable's numbers without writing into this shared
        /// asset — e.g. a utility item rescaling its modifier perk by its own (gem-bought) rank. Pass null
        /// for the plain, asset-only lookup.
        /// </summary>
        public SkillVariableData GetPerkVariable(string variableId, SkillVariableData externalOverride = null)
        {
            if (externalOverride != null && externalOverride.VariableId == variableId) return externalOverride;

            SkillVariableData own = PerkVariables != null ? PerkVariables.Find(v => v.VariableId == variableId) : null;
            if (own != null) return own;

            if (PerkConfig != null && PerkConfig.TryGetVariable(variableId, out SkillVariableData fromConfig)) return fromConfig;
            return null;
        }
        
        /// <summary>
        /// Builds the display description for a given rank by filling the description template's
        /// {Placeholders} from <see cref="PerkVariables"/>.
        ///
        /// The asset's Description is a template, e.g. "Increases Max Health by {HealthIncrease}".
        /// Every {Name} is looked up in PerkVariables by Name and replaced with that variable's value at
        /// this rank (BaseValue + ValuePerRank * rank, or just BaseValue when DisplayOnlyBaseValue),
        /// wrapped in the variable's TextColor as rich text, and suffixed with '%' when IsPercentage.
        /// A "{Name:format}" suffix applies any standard numeric format string (e.g. "{Damage:F1}").
        /// Placeholders with no matching variable are replaced with an empty string.
        ///
        /// A placeholder is resolved from PerkVariables first, then from the config (see
        /// PerkConfig.TryGetVariable) — so a skill-granting perk prints the granted skill's own numbers
        /// instead of keeping a second copy of them here.
        /// </summary>
        /// <param name="rank">Rank to show values for — usually the rank the player would end up at.</param>
        /// <param name="variableOverride">See <see cref="GetPerkVariable"/> — substitutes one variable's
        /// numbers (e.g. a utility item's gem-rank-scaled values) without touching this asset.</param>
        public string BuildDescription(int rank, SkillVariableData variableOverride = null)
        {
            string descriptionTemplate = GetDescription();
            var rx = new Regex(@"\{([A-Za-z_][A-Za-z0-9_]*)\s*(?::([^}]+))?\}", RegexOptions.Compiled);
            string result = rx.Replace(descriptionTemplate, m =>
            {
                string varName = m.Groups[1].Value;
                string fmt     = m.Groups[2].Success ? m.Groups[2].Value : null;

                SkillVariableData variable = GetPerkVariable(varName, variableOverride);
                if (variable == null) return "";

                float value = variable.GetDisplayValue(rank);
                if (variable.IsPercentage) value *= 100f;

                // Optional "{Name:format}" suffix — any standard numeric format string (F1, N0, ...).
                string valueString = string.IsNullOrEmpty(fmt)
                    ? value.Stringfy()
                    : value.ToString(fmt.Trim(), CultureInfo.InvariantCulture);
                if (variable.IsPercentage) valueString += '%';
                return "<color=#" + ColorUtility.ToHtmlStringRGBA(variable.TextColor) + ">" + valueString + "</color>";
            
            });
            return result;
        }

        public Perk CreatePerk()
        {
            if (string.IsNullOrEmpty(PerkClassName))
            {
                Debug.LogWarning($"PerkAsset ({name}): PerkClassName is empty — no perk created, so it will never be acquired or ranked up.");
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