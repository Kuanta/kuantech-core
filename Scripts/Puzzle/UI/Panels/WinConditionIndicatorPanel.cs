using System;
using System.Collections.Generic;
using Kuantech.DominoChain;
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

        [Header("Sizer")] 
        [SerializeField] private PanelSizer PanelSizer;
        
        public void SetIndicatorElements(WinConditionTracker tracker)
        {
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
            
            foreach (var pair in tracker.Targets)
            {
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
            
            //Set the size
            if(PanelSizer != null) PanelSizer.SetHorizontalElementCount(tracker.Targets.Count);
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