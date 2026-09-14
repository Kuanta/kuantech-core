using System.Collections.Generic;
using Kuantech.Core.UI;
using Kuantech.Midcore;
using Kuantech.Rpg.Skills;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.RogueLike
{
    public class PerkSelectionCard : UIElement
    {
        [Header("Components")]
        [SerializeField] private TMP_Text PerkName;
        [SerializeField] private TMP_Text PerkRank;
        [SerializeField] private TMP_Text PerkDescription;
        [SerializeField] private Image PerkIcon;

        [Header("Button")]
        [SerializeField] private Button SelectButton;

        public UnityAction<PerkSelectionData> OnSelect;

        private PerkSelectionData _selectionData;

        private void Start()
        {
            SelectButton.onClick.AddListener(OnClickedHandler);
        }  
        public void SetPerk(PerkSelectionData selectionData)
        {
            _selectionData = selectionData;
            if(PerkName != null) PerkName.text = _selectionData.PerkAsset.GetName();
            // BuildDescription, not GetDescription: the raw description is a template with {Placeholders}
            // that must be filled with this rank's values.
            //
            // Ask the game's progression for any overrides it would apply to this perk (e.g. an equipped,
            // ranked-up item that rescales the perk it grants) -- otherwise the card would show stale
            // asset-authored numbers while picking it actually applies the scaled ones.
            List<SkillVariableData> variableOverrides = ProgressionManager.GetPerkVariableOverrides(_selectionData.PerkAsset);
            if(PerkDescription != null) PerkDescription.text = _selectionData.PerkAsset.BuildDescription(_selectionData.PerkRank, variableOverrides);
            if(PerkRank != null) PerkRank.text = _selectionData.PerkRank.Stringfy();
            if(PerkIcon != null) PerkIcon.sprite = _selectionData.PerkAsset.GetIcon();
        }

        private void OnClickedHandler()
        {
            OnSelect?.Invoke(_selectionData);    
        }
    }
}