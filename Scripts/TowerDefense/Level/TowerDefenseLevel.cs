using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class TowerDefenseLevel : Level
    {
        [Header("Data")] 
        public TowerDefenseLevelData LevelData;
        
        //Phases
        private PreparationPhase _preparationPhase;
        private WavePhase _wavePhase;
        
        //Runtime
        [NonSerialized] public float TowerHealth;
        
        public override void SetupLevel()
        {
            Debug.Log("Setting Up Level");
            TowerHealth = LevelData.TowerHealth;
            base.SetupLevel();
            RegisterPhases();
            StartLevel();
        }

        private void RegisterPhases()
        {
            _preparationPhase = new PreparationPhase();
            _preparationPhase.PreparationTime = 3;
            _wavePhase = new WavePhase();
            
            PhaseSystem.RegisterPhase(_preparationPhase);
            PhaseSystem.RegisterPhase(_wavePhase);
        }
        
        protected override void PlayLevel()
        {
            Debug.Log("Playing Level");
            base.PlayLevel();
            Reset();
            
            //Start preparation phase
            PhaseSystem.ChangePhase(_preparationPhase);
        }

        protected override void Update()
        {
            base.Update();
            if (Input.GetKeyDown(KeyCode.F5))
            {
                RestartLevel();
            }
        }
        
        public override void ResetLevelState()
        {
            Reset();
        }
        
        public void CheckFailCondition()
        {
            if(TowerHealth <= 0) FailLevel();
        }

        private void Reset()
        {
            TowerHealth = LevelData.TowerHealth;
        }

        #region Events

        public void OnPreperationPhaseEnd()
        {
            PhaseSystem.ChangePhase(_wavePhase);
        }
        #endregion
    }
}