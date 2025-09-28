using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Utils;
using Kuantech.Midcore.UI;
using Kuantech.Rpg;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    public class PerkHandlerLevelModule : LevelModule
    {

        public List<PerkAsset> AvailablePerks; //All available perks to choose from
        public List<PerkData> PerkSelectionList; //For predefined order of perk selection
        private PerkHandler _perkHandler;
        private Queue<PerkData> _perkSelectionQueue;
        private List<PerkAsset> _availablePerks;
        private PerkSelectionPanel _perkSelectionPanel;

        public int PerkSelectionCount = 3;
        
        //Events
        public EventHandler<PerkData> OnPerkChosen;
        
        public override void Initialize()
        {
            base.Initialize();
            _perkHandler = new PerkHandler();
            _availablePerks = GetAvailablePerks();
            Reset();
            
            //Get reference to the perk selection panel
            _perkSelectionPanel = ParentLevel.LevelUI.GetUIElementByType<PerkSelectionPanel>();
            if (_perkSelectionPanel == null)
            {
                Debug.LogError("Add perk selection panel");
            }
            else
            {
                _perkSelectionPanel.OnPerkChosen -= OnPerkChosenHandler;
                _perkSelectionPanel.OnPerkChosen += OnPerkChosenHandler;
            }
        }

        public virtual List<PerkAsset> GetAvailablePerks()
        {
            return AvailablePerks;
        }
        
        public void AddPerk(PerkAsset perkAsset)
        {
            _perkHandler.AddPerk(perkAsset);
        }
        
        /// <summary>
        /// Shows the perk selection menu
        /// </summary>
        public void ShowPerkSelectionMenu()
        {
            List<PerkData> perkDatas = GetPerkSelectionHand(PerkSelectionCount);
            if (perkDatas.IsNullOrEmpty())
            {
                return;
            }
            _perkSelectionPanel.SetPerks(perkDatas);
            _perkSelectionPanel.Open();
        }
        
        private WeightedProbabilityArray<PerkAsset> _perkSelectionArray;
        /// <summary>
        /// Gets a selection of perks to choose from
        /// </summary>
        /// <returns></returns>
        public List<PerkData> GetPerkSelectionHand(int selectionCount)
        {
            List<PerkData> perkDatas = new List<PerkData>(selectionCount);
            for (int i = 0; i < selectionCount; ++i)
            {
                if (!_perkSelectionQueue.IsNullOrEmpty())
                {
                    perkDatas.Add(_perkSelectionQueue.Dequeue());
                }
                else
                {
                    perkDatas.Add(GetRandomPerkData());
                }
            }

            return perkDatas;
        }

        public WeightedProbabilityArray<PerkAsset> GeneratePerkSelectionArray()
        {
            WeightedProbabilityArray<PerkAsset> probArray = new WeightedProbabilityArray<PerkAsset>();
            float probDecayPerRank = ConfigManager.GetFloatConfig("ProbDecayPerRank", 0.5f);
            foreach (var perks in AvailablePerks)
            {
                int currentRank = _perkHandler.GetCurrentPerkRank(perks);
                float weight = Mathf.Pow(probDecayPerRank, currentRank); //1 * 0.5^Rank
                probArray.AddElement(perks, weight);
            }

            return probArray;
        }
        public PerkData GetRandomPerkData()
        {
            if (_perkSelectionArray == null || _perkSelectionArray.IsNullOrEmpty())
            {
                _perkSelectionArray = GeneratePerkSelectionArray();
            }

            PerkAsset perkAsset = _perkSelectionArray.Sample();
            _perkSelectionArray.RemoveElement(perkAsset);
            return new PerkData()
            {
                PerkAsset = perkAsset,
                CurrentRank = _perkHandler.GetCurrentPerkRank(perkAsset) + 1,
            };
        }
        
        public override void OnReset()
        {
            base.OnReset();
            Reset();
        }

        private void Reset()
        {
            _perkHandler.ClearPerks();
            _perkSelectionQueue = new Queue<PerkData>();
            if (!PerkSelectionList.IsNullOrEmpty())
            {
                foreach (var perkData in PerkSelectionList)
                {
                    _perkSelectionQueue.Enqueue(perkData);
                }
            }
        }

        private void OnPerkChosenHandler(object sender, PerkData selectedPerk)
        {
            //Add perk
            AddPerk(selectedPerk.PerkAsset);
            
            OnPerkChosen?.Invoke(this, selectedPerk);
        }
    }
}