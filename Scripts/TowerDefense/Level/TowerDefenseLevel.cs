using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class TowerDefenseLevel : Level
    {
        [Header("Data")] 
        public TowerDefenseLevelData LevelData;

        [Header("Components")] 
        public List<TowerDefensePath> Paths;
        
        //Phases
        private PreparationPhase _preparationPhase;
        private WavePhase _wavePhase;
        
        //Runtime
        [NonSerialized] public float TowerHealth;
        public List<ActorSummoner> ActorSummoners = new List<ActorSummoner>();
        
        public override void SetupLevel()
        {
            Debug.Log("Setting Up Level");
            TowerHealth = LevelData.TowerHealth;
            base.SetupLevel();
            ActorSummoners = GetComponentsInChildren<ActorSummoner>().ToList();
            Paths = GetComponentsInChildren<TowerDefensePath>().ToList();
            foreach (var path in Paths)
            {
                path.Initialize();
            }
            RegisterPhases();
            StartLevel();
        }

        private void RegisterPhases()
        {
            _preparationPhase = new PreparationPhase();
            _preparationPhase.PreparationTime = 1;
            _wavePhase = new WavePhase();
            
            PhaseSystem.RegisterPhase(_preparationPhase);
            PhaseSystem.RegisterPhase(_wavePhase);
        }
        
        #region Level Lifecycle
        protected override void PlayLevel()
        {
            Debug.Log("Playing Level");
            base.PlayLevel();
            Reset();
            ToggleSpawners(false);
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

        private void Reset()
        {
            TowerHealth = LevelData.TowerHealth;
        }
        #endregion

        #region Wave Control
        
        /// <summary>
        /// Toggles spawners
        /// </summary>
        /// <param name="toggle"></param>
        public void ToggleSpawners(bool toggle)
        {
            foreach (var spawner in ActorSummoners)
            {
                spawner.ToggledOn = toggle;
            }
        }

        #endregion
        
        #region Win or Lose
        public void CheckFailCondition()
        {
            if(TowerHealth <= 0) FailLevel();
        }
        #endregion



        #region Events
        public void OnPreperationPhaseEnd()
        {
            PhaseSystem.ChangePhase(_wavePhase);
        }
        #endregion
    }
}