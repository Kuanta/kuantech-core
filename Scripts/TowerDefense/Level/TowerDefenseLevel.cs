using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using Kuantech.Core.Store;
using Kuantech.Midcore;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class TowerDefenseLevel : Level
    {
        [Header("Data")] 
        public TowerDefenseLevelData LevelData;
        public CurrencyAsset StartingCurrencyAsset;
        public SpawnablesCollection SpawnablesCollection;
        
        [Header("Components")] 
        public List<TowerDefensePath> Paths;
        
        //Phases
        private PreparationPhase _preparationPhase;
        private WavePhase _wavePhase;
        
        //Runtime
        [NonSerialized] private TowerDefenseLevelUI _towerDefenseLevelUI;
        [NonSerialized] public float TowerHealth;
        public List<ActorSummoner> ActorSummoners = new List<ActorSummoner>();
        
        public HashSet<Actor> AliveEnemies = new HashSet<Actor>();
        private Queue<WaveEntry> _currentWaveQueue = new Queue<WaveEntry>();
        private float _lastWaveSpawnTime = 0f;
        private int _remainingEnemyCount = 0;
        private int CurrentWaveIndex = 0;
        
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
            Reset();
            //SetupWaveQueue();
            base.PlayLevel();
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

            if (CurrentState != LevelState.Playing) return;
            SpawnNextWaveElement();
        }
        
        public override void ResetLevelState()
        {
            base.ResetLevelState();
            Reset();
        }

        private void Reset()
        {
            SetTowerHealth(GetMaxTowerHealth());
            CurrentWaveIndex = -1; //-1 so set next wave works
            //Set the starting gold
            CurrencyManager.SetCurrency(StartingCurrencyAsset, LevelData.StartingGold);
        }
        #endregion

        #region Wave Control

        public void SetNextWave()
        {
            SetWave(CurrentWaveIndex+1);
        }
        
        /// <summary>
        /// Sets the wave data
        /// </summary>
        /// <param name="waveIndex"></param>
        public void SetWave(int waveIndex)
        {
            if (waveIndex >= LevelData.WaveData.Count) return;
            WaveData waveData = LevelData.WaveData[waveIndex];
            CurrentWaveIndex = waveIndex;
            _currentWaveQueue = new Queue<WaveEntry>();
            foreach (var entry in waveData.WaveEntries)
            {
                _currentWaveQueue.Enqueue(entry);
            }
            SetRemainingEnemyCount(waveData.EnemyCount);
        }
        
        /// <summary>
        /// Sets the remaining enemy count for the wave
        /// </summary>
        /// <param name="remainingEnemyCount"></param>
        public void SetRemainingEnemyCount(int remainingEnemyCount)
        {
            _remainingEnemyCount = remainingEnemyCount;
        }
        
        /// <summary>
        /// Gets the remnai
        /// </summary>
        /// <returns></returns>
        public int GetRemainingEnemyCount()
        {
            return _remainingEnemyCount;
        }

        public int GetMaxEnemyCount()
        {
            return GetCurrentWaveData().EnemyCount;
        }
        public WaveData GetCurrentWaveData()
        {
            return LevelData.WaveData[CurrentWaveIndex];
        }
        public bool IsActorEnemy(Actor actor)
        {
            return actor.FactionId > 0;
        }

        public void SpawnNextWaveElement()
        {
            if (GetCurrentPhase() != _wavePhase || _remainingEnemyCount <= 0) return;
            
            //Check cooldown
            if (Time.time - _lastWaveSpawnTime < GetCurrentWaveData().WaveSpawnDelay)
            {
                return;
            }
            
            //Peek next data
            WaveEntry entry = GetNextWaveEntry();
            ActorSummoner summoner = GetSummoner(entry.SpawnerIndex);
            ActorTemplateAsset actorToSpawn = GetActorTemplate(entry.SpawnableIndex);
            if (actorToSpawn == null) return;
            Actor spawned = summoner.SpawnActor(actorToSpawn);
            if (spawned == null) return;
            
            _lastWaveSpawnTime = Time.time;
            _remainingEnemyCount--;
        }

        public ActorSummoner GetSummoner(int index)
        {
            if (ActorSummoners.IsNullOrEmpty())
            {
                return null;
            }

            index = index % ActorSummoners.Count;
            return ActorSummoners[index];
        }

        public ActorTemplateAsset GetActorTemplate(int index)
        {
            return SpawnablesCollection.GetActorTemplate(index);
        }
        
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

        public WaveEntry GetNextWaveEntry()
        {
            if (_currentWaveQueue.IsNullOrEmpty())
            {
                int enemyIndex = 0;

                if (!GetCurrentWaveData().EnemyProbabilities.Elements.IsNullOrEmpty())
                {
                    enemyIndex = GetCurrentWaveData().EnemyProbabilities.Sample();
                }
                //Generate Random
                return new WaveEntry()
                {
                    SpawnerIndex = enemyIndex,
                    SpawnableIndex = UnityEngine.Random.Range(0, ActorSummoners.Count),
                };
            }

            return _currentWaveQueue.Dequeue();
        }
        
        /// <summary>
        /// Completes the current wave
        /// </summary>
        public void CompleteWave()
        {
            if (CurrentWaveIndex >= LevelData.WaveData.Count - 1)
            {
                CompleteLevel();
                return;
            }
            PhaseSystem.ChangePhase(_preparationPhase);
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

        public void CheckWaveCompletion()
        {
            if (_remainingEnemyCount > 0) return;
            if (!_currentWaveQueue.IsNullOrEmpty()) return;
            foreach(var enemy in AliveEnemies)
            {
                if (enemy != null || enemy.IsAlive())
                {
                    return;
                }
            }
            
            //Level is won
            CompleteWave();
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

        public override void AddSpawnable(ISpawnable spawnable)
        {
            base.AddSpawnable(spawnable);
            if (spawnable is Actor actor && IsActorEnemy(actor))
            {
                AliveEnemies ??= new HashSet<Actor>();
                AliveEnemies.Add(actor);
            }
        }
        
        public void OnActorDeath(Actor actor)
        {
            if (IsActorEnemy(actor) && AliveEnemies.Contains(actor))
            {
                AliveEnemies.Remove(actor);
            }
            CheckWaveCompletion();
        }

        public void OnActorDespawn(Actor actor)
        {
            // if (IsActorEnemy(actor) && AliveEnemies.Contains(actor))
            // {
            //     AliveEnemies.Remove(actor);
            // }
            // CheckWaveCompletion();
        }
        #endregion
    }
}