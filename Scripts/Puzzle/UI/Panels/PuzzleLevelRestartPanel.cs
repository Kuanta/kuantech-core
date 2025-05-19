using DG.Tweening;
using Kuantech.Core;
using Kuantech.Core.UI;
using Kuantech.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Puzzle.UI
{
    public class PuzzleLevelRestartPanel : UIMenu
    {
        [SerializeField] private Button RestartButton;
        public float InitialScale = 0.5f;
        public float DOTweenTime = 0.5f;
        private void Start()
        {
            RestartButton.onClick.AddListener(()=>{
                Level level = LevelManager.GetContext<LevelManager>().CurrentLevel;
                if(level == null) return;
                level.RestartLevel();
                Close();
            });
        }

        public override void Open()
        {
            base.Open();
            transform.localScale = Vector3.one * InitialScale;
            transform.DOScale(Vector3.one, DOTweenTime);
        }
    }
}