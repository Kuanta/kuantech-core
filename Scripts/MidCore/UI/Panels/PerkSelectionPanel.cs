using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.UI;
using Kuantech.Rpg;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class PerkSelectionPanel : UIElement
    {
        [SerializeField] private RectTransform PerkSelectionContainer;
        [SerializeField] private PerkSelectionElement PerkSelectionElementPrefab;
        
        private List<PerkSelectionElement> _perkSelectionElements = new List<PerkSelectionElement>();
        
        //Events
        public EventHandler<PerkData> OnPerkChosen;

        public override void Open()
        {
            base.Open();
            GameManager.PauseGame();
        }

        public override void Hide()
        {
            base.Hide();
            GameManager.ResumeGame();
        }
        
        public void SetPerks(List<PerkData> perkDatas)
        {
            Helpers.DestroyAllChildren(PerkSelectionContainer);

            foreach (var data in perkDatas)
            {
                PerkSelectionElement element = Instantiate(PerkSelectionElementPrefab, PerkSelectionContainer);
                element.Initialize();
                element.ParentPanel = this;
                element.SetPerk(data);
                _perkSelectionElements.Add(element);
            }
        }
        
        public void OnPerkSelected(PerkSelectionElement perkSelectionElement)
        {
            foreach(var _perkSelectionElement in _perkSelectionElements)
            {
                if(_perkSelectionElement == perkSelectionElement)
                {
                    _perkSelectionElement.PlaySelectedAnimation();
                }
                else
                {
                    _perkSelectionElement.PlayNotSelectedAnimation();
                }
            }
            
            //Notify the perk handler level module of the selected perk
            OnPerkChosen?.Invoke(this, perkSelectionElement.CurrentPerkData);
            Close(); //Close the panel
        }
    }
}