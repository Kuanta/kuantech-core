using TMPro;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.UI
{
    /// <summary>
    /// An indicator that shows the current level index. Not useful for games that doesn't have a level system
    /// </summary>
    public class LevelIndicator
    {
        [SerializeField] private TMP_Text LevelIndexText;
        [SerializeField] private string LevelPrefix = "Level";

        private void Start()
        {
            LevelManager levelMan = (GameManager.Instance.GetSubManagerByType<LevelManager>() 
            as LevelManager);
            if(levelMan == null) return;
            levelMan.LevelSetEvent += OnLevelSetEvent; 
        }

        private void OnDestroy()
        {
            LevelManager levelMan = (GameManager.Instance.GetSubManagerByType<LevelManager>()
                        as LevelManager);
            if (levelMan == null) return;
            levelMan.LevelSetEvent -= OnLevelSetEvent;
        }

        private void OnEnable()
        {
            LevelManager levelMan = (GameManager.Instance.GetSubManagerByType<LevelManager>()
                        as LevelManager);
            if (levelMan == null) return;
            SetLevelIndex(levelMan.CurrentLevelIndex);
        }

        private void SetLevelIndex(int levelIndex)
        {
            LevelIndexText.text = $"{LevelPrefix} {(levelIndex + 1).ToString()}";
        }

        private void OnLevelSetEvent(object sender, int levelIndex)
        {
            SetLevelIndex(levelIndex);
        }
    }
}