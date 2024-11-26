using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.UI
{
    public class StageIndicator : MonoBehaviour
    {
        public StageIndicatorElement IndicatorPrefab;
        public StageIndicatorElement BackgroundIndicatorPrefab;
        public List<StageIndicatorElement> IndicatorElements;
        public List<StageIndicatorElement> IndicatorElementsBackground;
        public RectTransform ParentRectTransform;
        public RectTransform BackgroundRectTransform;
        
        public void SetupForLevel(int stageCount)
        {
            if (!IndicatorElements.IsNullOrEmpty())
            {
                foreach (var element in IndicatorElements)
                {
                    Destroy(element.gameObject);
                }
            }

            if (!IndicatorElementsBackground.IsNullOrEmpty())
            {
                foreach (var element in IndicatorElementsBackground)
                {
                    Destroy(element.gameObject);
                }
            }
            IndicatorElements = new List<StageIndicatorElement>();
            IndicatorElementsBackground = new List<StageIndicatorElement>();
            
            //Backgrounds
            for (int i = 0; i < stageCount; ++i)
            {
                StageIndicatorElement indicatorElement = Instantiate(BackgroundIndicatorPrefab, BackgroundRectTransform, true);
                indicatorElement.transform.localScale = Vector3.one;
                bool shouldShowConnection = stageCount > 0 && i != stageCount - 1;
                indicatorElement.ToggleConnection(shouldShowConnection);
                IndicatorElementsBackground.Add(indicatorElement);
            }
            
            for (int i = 0; i < stageCount; ++i)
            {
                StageIndicatorElement indicatorElement = Instantiate(IndicatorPrefab, ParentRectTransform, true);
                indicatorElement.transform.localScale = Vector3.one;
                IndicatorElements.Add(indicatorElement);
                bool shouldShowConnection = stageCount > 0 && i != stageCount - 1;
                indicatorElement.ToggleConnection(shouldShowConnection);
            }
            
        }

        public void SetCurrentStage(int currentStage)
        {
            for (int i = 0; i < IndicatorElements.Count; ++i)
            {
                IndicatorElements[i].SetFill(i <= currentStage);
            }
        }
    }
}