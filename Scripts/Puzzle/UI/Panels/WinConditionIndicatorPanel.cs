using System;
using System.Collections.Generic;
using Kuantech.Utils.UI;
using UnityEngine;

namespace Kuantech.Puzzle.UI
{
    public class WinConditionIndicatorPanel : MonoBehaviour
    {
        [Serializable]
        public struct IndicatorSpriteEntry
        {
            public string Key;
            public ColoredSpriteAsset Sprite;
        }
        
        public GameObject EntriesParent;
        public WinConditionIndicatorElement EntryElementPrefab;
        public List<IndicatorSpriteEntry> Sprites;
        public Dictionary<string, WinConditionIndicatorElement> IndicatorElements;

        [Header("Stages")] 
        [SerializeField] private LevelStagesPanel StagesPanel;
        
        [Header("Sizer")] 
        [SerializeField] private PanelSizer PanelSizer;

        private WinConditionTracker _tracker;
        private int _currentlyShownStage = 0;
        public void SetTracker(WinConditionTracker tracker)
        {
            _tracker = tracker;
            SetStageCount(_tracker.GetStageCount());
        }
        
        // /// <summary>
        // /// Sets the panel for the current stage
        // /// </summary>
        // public void SetPanelForCurrentStage()
        // {
        //     int currentStageIndex = _tracker.GetCurrentStageIndex();
        //     SetPanelForStage(currentStageIndex);
        // }

        public void SetPanelForStage(int stageIndex)
        {
            if (_tracker == null)
            {
                Debug.LogWarning("Targets panel couldn't be set");
                return;
            }
            //Clear previous ones
            if (IndicatorElements != null)
            {
                foreach (var pair in IndicatorElements)
                {
                    Destroy(pair.Value.gameObject);
                }
                IndicatorElements.Clear();
            }
            else
            {
                IndicatorElements = new Dictionary<string, WinConditionIndicatorElement>();
            } 
            
            PuzzleLevelStage currentStage = _tracker.GetStage(stageIndex);
            SetCurrentStageIndex(_tracker.GetCurrentStageIndex());
            foreach (var pair in currentStage.Targets)
            {
                if (pair.Value.TargetAmount <= 0) continue;
                string targetKey = pair.Key;
                WinConditionIndicatorElement element = Instantiate(EntryElementPrefab);
                IndicatorElements[targetKey] = element;
                element.ShowRemaining = pair.Value.ShowRemaining;
                element.SetIcon(GetIconFromKey(targetKey));
                element.SetScore(pair.Value.TargetAmount, pair.Value.TargetAmount);
                element.transform.SetParent(EntriesParent.transform);
                element.transform.localPosition = Vector3.zero;
                element.transform.localRotation = Quaternion.identity;
                element.transform.localScale = Vector3.one;
            }

            _currentlyShownStage = _tracker.GetCurrentStageIndex();
            //Set the size
            if(PanelSizer != null) PanelSizer.SetHorizontalElementCount(currentStage.Targets.Count);
        }
        
        /// <summary>
        /// Sets the ui elements for the stage index
        /// </summary>
        /// <param name="stageIndex"></param>
        public virtual void SetCurrentStageIndex(int stageIndex)
        {
            StagesPanel.SetStage(stageIndex);
        }
        
        /// <summary>
        /// Sets the stage count
        /// </summary>
        /// <param name="stageCount"></param>
        public virtual void SetStageCount(int stageCount)
        {
            StagesPanel.SetStageCount(stageCount);
        }
        
        public void SetScore(string key, int currentAmount, int remainingAmount)
        {
            if (!IndicatorElements.ContainsKey(key)) return;
            IndicatorElements[key].SetScore(currentAmount, remainingAmount);
        }
        
        private ColoredSpriteAsset GetIconFromKey(string key)
        {
            foreach (var spriteEntry in Sprites)
            {
                if (spriteEntry.Key == key) return spriteEntry.Sprite;
            }
            return null;
        }
    }
}