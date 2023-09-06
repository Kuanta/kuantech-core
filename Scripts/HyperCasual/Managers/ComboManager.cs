using Cysharp.Threading.Tasks;
using Kuantech.Core.HyperCasual.UI;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class ComboManager : SubManager
    {
        public float ComboMultiplierIncrement = 0.5f;
        public int ComboMultiplierIncreaseThresh = 5; //Every this combo, increase combo multiplier by ComboMultiplier
        private int _currentComboCount;
        [SerializeField] private ComboIndicator ComboIndicator;

        
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            (ParentManager as HCGameManager).StateChangeEvent += OnStateChange;
            UIManager uiManager = ParentManager.GetSubManagerByType<UIManager>() as UIManager;
        }
        
        public void IncreaseComboCount()
        {
            _currentComboCount++;
            UpdateComboUI();
        }

        public float GetComboMultiplier()
        {
            int amount = Mathf.FloorToInt(_currentComboCount / (float)ComboMultiplierIncreaseThresh);
            return 1 + amount * ComboMultiplierIncrement;
        }
        
        public void ResetCombo()
        {
            _currentComboCount = 0;
            UpdateComboUI();
        }

        private void UpdateComboUI()
        {
            if (ComboIndicator == null) return;
            ComboIndicator.SetComboCounter(_currentComboCount);
        }

        private void OnStateChange(object sender, StateChangeData stateChangeData)
        {
            if (stateChangeData.NewState != LevelState.Waiting) return;
            ResetCombo();
        }
    }
}