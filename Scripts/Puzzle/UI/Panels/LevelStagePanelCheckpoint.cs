using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Puzzle.UI
{
    public class LevelStagePanelCheckpoint : MonoBehaviour
    {
        [SerializeField] private GameObject ReachedState;
        [SerializeField] private GameObject UnReachedState;
        [SerializeField] private TMP_Text NumberText;

        private bool _reached = false;
        public void SetStageNumber(int stageIndex)
        {
            NumberText.text = (stageIndex+1).Stringfy();
        }

        public void ToggleReached(bool reached)
        {
            _reached = reached;
            ReachedState.SetActive(reached);
            UnReachedState.SetActive(!reached);
        }
    }
}