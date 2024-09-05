using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Puzzle.UI
{
    public class LevelStagesPanel : MonoBehaviour
    {
        [Header("Fillbar")] 
        [SerializeField] private RectTransform FillbarParent;
        [SerializeField] private Slider Fillbar;

        [Header("Checkpoints")] [SerializeField]
        private Transform CheckpointsParent;
        [SerializeField] private LevelStagePanelCheckpoint CheckpointPrefab;
        
        [Tooltip("Distance between each Stage")]
        [SerializeField] private float StagesPadding;

        private List<LevelStagePanelCheckpoint> _checkpoints;
        private int _stageCount;
        private int _currentStage = 0;
        
        public void SetStageCount(int stageCount)
        {
            if (stageCount <= 1)
            {
                FillbarParent.sizeDelta = new Vector2(0, FillbarParent.sizeDelta.y);
            }

            if (!_checkpoints.IsNullOrEmpty())
            {
                //Could have found a better way but im lazy
                foreach (var cp in _checkpoints)
                {
                    Destroy(cp.gameObject);
                }
            }

            _stageCount = stageCount;
            float parentSize = FillbarParent.rect.width;
            StagesPadding = parentSize / Mathf.Max((_stageCount - 1), 0.0f);
            //FillbarParent.sizeDelta = new Vector2((stageCount - 1) * StagesPadding, FillbarParent.sizeDelta.y);
            _checkpoints = new List<LevelStagePanelCheckpoint>();
            for (int i = 0; i < stageCount; ++i)
            {
                float position = i * StagesPadding;
                LevelStagePanelCheckpoint cp = Instantiate(CheckpointPrefab);
                cp.SetStageNumber(i);
                cp.transform.SetParent(CheckpointsParent);
                cp.transform.localScale = Vector3.one;
                cp.transform.localRotation = Quaternion.identity;
                RectTransform rectTransform = cp.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 0.5f);
                rectTransform.anchoredPosition = new Vector2(position, 0);
                cp.SetStageNumber(i);
                _checkpoints.Add(cp);
            }
        }

        public void SetStage(int stage)
        {
            Debug.LogError($"Setting stage UI for stage {stage}");
            if (_stageCount <= 1)
            {
                Fillbar.value= 0;
                return;
            }

            for (int i = 0; i < _stageCount; ++i)
            {
                _checkpoints[i].ToggleReached(i <= stage);
            }
            float fill = Mathf.Clamp01(stage / (float)(_stageCount - 1));
            Fillbar.value= fill;
        }
        
        /// <summary>
        /// Sets the fillbar for the current stage progress
        /// </summary>
        /// <param name="progress">Normalized current stage progress</param>
        /// <returns></returns>
        public void SetCurrentStageProgress(float progress)
        {
            float fillPerStage = 1.0f / (float) _stageCount;
            float fillForStage = fillPerStage * progress;

            float finalFill = _currentStage * fillPerStage + fillForStage;
            Fillbar.value = finalFill;
        }
    }
}