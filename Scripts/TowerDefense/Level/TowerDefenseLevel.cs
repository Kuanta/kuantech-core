using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using Kuantech.Core.Store;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class TowerDefenseLevel : Level
    {
        [Header("Data")] 
        public TowerDefenseLevelData LevelData;
        public CurrencyAsset StartingCurrencyAsset;
        
        [Header("Components")] 
        public List<TowerDefensePath> Paths;
        
        //Phases
        private PreparationPhase _preparationPhase;
        private WavePhase _wavePhase;
        
        //Runtime
        [NonSerialized] private TowerDefenseLevelUI _towerDefenseLevelUI;
        [NonSerialized] public float TowerHealth;
        public List<ActorSummoner> ActorSummoners = new List<ActorSummoner>();
        
        public override void SetupLevel()
        {
            TowerHealth = LevelData.TowerHealth;
            base.SetupLevel();
            _towerDefenseLevelUI =LevelUI as TowerDefenseLevelUI;
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
            base.ResetLevelState();
            Reset();
        }

        private void Reset()
        {
            SetTowerHealth(GetMaxTowerHealth());
                        
            //Set the starting gold
            CurrencyManager.SetCurrency(StartingCurrencyAsset, LevelData.StartingGold);
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

        public void ReceiveTowerDamage(int damage)
        {
            SetTowerHealth(GetTowerHealth()-damage);
            if (GetTowerHealth() <= 0)
            {
                FailLevel();
            }
        }

        public void SetTowerHealth(float towerHealth)
        {
            TowerHealth = towerHealth;
            if (_towerDefenseLevelUI != null)
            {
                _towerDefenseLevelUI.SetHealthText(GetTowerHealth(), GetMaxTowerHealth());
            }
        }
    
        public float GetMaxTowerHealth()
        {
            return LevelData.TowerHealth;
        }
        
        public float GetTowerHealth()
        {
            return TowerHealth;
        }
        
        public void CheckFailCondition()
        {
            if(GetTowerHealth() <= 0) FailLevel();
        }

        public override void FailLevel()
        {
            base.FailLevel();
            
            //Halt Tower defense level elements
            foreach (var spawnable in SpawnedActors)
            {
                if(spawnable == null) continue;
                if (spawnable is Actor actor)
                {
                    actor.ChangeActorState(ActorState.Inactive);
                }
            }
        }
        #endregion

        #region Events
        public void OnPreperationPhaseEnd()
        {
            PhaseSystem.ChangePhase(_wavePhase);
        }
        
        public void OnActorReachedEnd(Actor actor)
        {
            if (actor.FactionId > 0)
            {
                ReceiveTowerDamage(1);
            }
        }
        #endregion
    }
}