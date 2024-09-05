using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Puzzle.UI
{
    public class LevelStagePanelCheckpoint : MonoBehaviour
    {
        [SerializeField] private GameObject ReachedState;
        [SerializeField] private GameObject UnReachedState;
        [SerializeField] private GameObject CompletedState;
        [SerializeField] private TMP_Text NumberText;

        public void SetStageNumber(int stageIndex)
        {
            NumberText.text = (stageIndex+1).Stringfy();
        }
        
        /// <summary>
        /// Sets the state of this element.
        /// </summary>
        /// <param name="stageIndex">Stage index this element represents</param>
        /// <param name="currentStage">Current stage index</param>
        public void SetState(int stageIndex, int currentStage)
        {
            ReachedState.SetActive(stageIndex == currentStage);
            UnReachedState.SetActive(stageIndex > currentStage);
            CompletedState.SetActive(stageIndex<currentStage);
        }
    }
}