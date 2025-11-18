using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Utils;
using Kuantech.RealTimeStrategy;
using Kuantech.Rpg;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.TowerDefense
{
    public class WaveHandlerModule : LevelModule
    {   
        [Header("Properties")]
        public int EnemyFactionId = 1;
        public float WaveCompletedDelay = 0.5f;
        
        [Header("Summoners")]
        public List<ActorSummoner> ActorSummoners;
        public SpawnablesCollection SpawnablesCollection;

        [Header("Wave Data")] 
        public List<WaveData> WaveDatas;

        [Header("Max Units Factor")] 
        public float BaseMaxUnitsFactor = 0.5f;
        public float MaxUnitsFactorIncreasePerKill = 0.05f;
        private float _currentMaxUnitsFactor;
        
        //Runtime
        [NonSerialized] public int CurrentWaveIndex;
        [NonSerialized] public bool WaveStarted;
        private UnitsManager _unitManager;
        private Queue<WaveEntry> _waveQueue;
        private float _lastSpawnTime;
        
        private Queue<ActorSummonData> _pendingSummons = new Queue<ActorSummonData>();
        
        //Events
        public UnityAction<Actor> OnEnemySpawned;
        public UnityAction OnWaveCompleted;
        
        public override void Initialize()
        {
            base.Initialize();
            if (Helpers.IsNullOrEmpty(ActorSummoners))
            {
                Debug.LogError("No Actor Summoners assigned to WaveHandlerModule!");
            }
            if (SpawnablesCollection == null)
            {
                Debug.LogError("No Spawnables Collection assigned to WaveHandlerModule!");
            }
            if (Helpers.IsNullOrEmpty(WaveDatas))
            {
                Debug.LogError("No Wave Data assigned to WaveHandlerModule!");
            }
            CurrentWaveIndex = -1;

            BaseMaxUnitsFactor = ConfigManager.GetFloatConfig("BaseMaxUnitsFactor", BaseMaxUnitsFactor);
            
            StopWave(); //Start as stopped
        }
        
        public override void PostLevelSetup()
        {
            base.PostLevelSetup();
            _unitManager = ParentLevel.GetLevelModule<UnitsManager>();
            _unitManager.OnActorRemoved += OnActorRemoved;
        }
        
        public void SetWaveDatas(List<WaveData> waveDatas,  int waveCount)
        {
            if (waveDatas.IsNullOrEmpty())
            {
                WaveGeneratorConfig config = TowerDefenseLevelDataManager.GetWaveGeneratorConfig();
                WaveDatas = WaveGenerator.Generate(config, SpawnablesCollection, ParentLevel.GetPowerLevel(), waveCount);
            }
            else
            {
                WaveDatas = waveDatas;
            }
        }
        
        public override void OnReset()
        {
            base.OnReset();
            _pendingSummons = null;
            CurrentWaveIndex = -1;
            _waveCompleteRoutine = null;
            _currentMaxUnitsFactor = BaseMaxUnitsFactor;
        }
        
        private void Update()
        {
            if (ParentLevel == null || ParentLevel.CurrentState != LevelState.Playing) return;
            if (WaveStarted)
            {
                SpawnNextWaveElement();
            }
        }

        #region Queries
        /// <summary>
        /// Checks if all wave are completed
        /// </summary>
        /// <returns></returns>
        public bool AreAllWavesCompleted()
        {
            return CurrentWaveIndex >= WaveDatas.Count - 1;
        }

        /// <summary>
        /// Checks if the current wave is completed.
        /// </summary>
        /// <returns></returns>
        public bool IsWaveCompleted()
        {
            int remainingEnemies = GetRemainingEnemyCount(EnemyFactionId);
            HashSet<Actor> enemyActors = _unitManager.GetActorsByFaction(EnemyFactionId);
            int aliveEnemies = enemyActors.Count;
            return remainingEnemies + aliveEnemies <= 0;
        }
        
        /// <summary>
        /// Checks if level is in wave phase
        /// </summary>
        /// <returns></returns>
        public bool IsLevelInWavePhase()
        {
            return ParentLevel.GetCurrentPhase() is WavePhase;
        }
        
        /// <summary>
        /// Gets current wave index
        /// </summary>
        /// <returns></returns>
        public int GetCurrentWaveIndex()
        {
            return Mathf.Max(CurrentWaveIndex, 0); //return 0 if its -1 too
        }
        
        /// <summary>
        /// Returns the number of total waves
        /// </summary>
        /// <returns></returns>
        public int GetWaveCount()
        {
            return WaveDatas.Count;
        }
        
        /// <summary>
        /// Gets current wave data
        /// </summary>
        /// <param name="waveIndex"></param>
        /// <returns></returns>
        public WaveData GetWaveDataForWave(int waveIndex)
        {
            waveIndex = Mathf.Clamp(waveIndex, 0, WaveDatas.Count-1);
            return WaveDatas[waveIndex];
        }

        #endregion
        
        #region Wave Control

        public void StartWave()
        {
            ToggleSpawners(true);
            WaveStarted = true;
            
        }

        public void StopWave()
        {
            ToggleSpawners(false);
            WaveStarted = false;
        }
        
        public void ToggleSpawners(bool toggle)
        {
            foreach (var spawner in ActorSummoners)
            {
                spawner.Toggled = toggle;
            }
        }
        
        public void SetNextWave()
        {
            SetWave(CurrentWaveIndex+1);
        }

        [Button("Set Wave")]
        public void SetWave(int waveIndex)
        {
            if (waveIndex >= WaveDatas.Count)
            {
                return;
            }

            WaveData waveData = WaveDatas[waveIndex];
            _waveQueue = new Queue<WaveEntry>();
            CurrentWaveIndex = waveIndex;
            foreach (var entry in waveData.WaveEntries)
            {
                _waveQueue.Enqueue(entry);
            }

            WeightedProbabilityArray<int> enemyProbs = new WeightedProbabilityArray<int>();
            if (Helpers.IsNullOrEmpty(waveData.EnemyProbabilities.Values) ||
                Helpers.IsNullOrEmpty(waveData.EnemyProbabilities.Weights))
            {
                enemyProbs.AddElement(0, 1);
            }
            else
            {
                for (int i = 0;
                     i < Mathf.Min(waveData.EnemyProbabilities.Values.Count,
                         waveData.EnemyProbabilities.Weights.Count);
                     ++i)
                {
                    enemyProbs.AddElement(waveData.EnemyProbabilities.Values[i],
                        waveData.EnemyProbabilities.Weights[i]);
                }
            }
            for(int i=0;i<waveData.GeneratedEnemyCount; i++)
            {
                int spawnableIndex = enemyProbs.Sample();
                _waveQueue.Enqueue(new WaveEntry
                {
                    SpawnableIndex = spawnableIndex,
                    SpawnerIndex = -1, //-1 means random spawner
                    Amount = 1,
                });
            }

            _pendingSummons = new Queue<ActorSummonData>();
            _currentMaxUnitsFactor = BaseMaxUnitsFactor;
            //Set actor limits
            UnitsManager um = ParentLevel.GetLevelModule<UnitsManager>();
            if (um != null)
            {
                um.SetMaxUnitPerFaction(waveData.EnemyFactionId, waveData.MaxEnemyCount);
                um.SetMaxUnitFactorPerFaction(EnemyFactionId, _currentMaxUnitsFactor);
            }

            _waveCompleteRoutine = null;
        }

        #endregion
        
        #region Summoners
 
        public void SpawnNextWaveElement()
        {
            while (!Helpers.IsNullOrEmpty(_pendingSummons))
            {
                ActorSummonData summonData = _pendingSummons.Peek();
                if (!CanSpawnActorBlueprint(summonData.ActorBlueprint))
                {
                    return; //Don't continue if there are actors pending
                }

                _pendingSummons.Dequeue();
                SpawnActor(summonData.ActorBlueprint, summonData.SummonerIndex, GetCurrentWaveData().WaveActorsLevel, summonData.Order);
            }
            
            WaveEntry nextEntry = PeekNextWaveEntry();
            if (!CanSpawnEnemy(nextEntry)) return;
            if (nextEntry.SpawnableIndex < 0) return;

            nextEntry = GetNextWaveEntry(); // Can summon, now pop from queue
            ActorSummoner summoner = GetSummoner(nextEntry.SpawnerIndex);
            int amount = Mathf.Max(1, nextEntry.Amount);
            for (int i = 0; i < amount; ++i)
            {
                ActorBlueprint actorBlueprint = GetActorTemplate(nextEntry.SpawnableIndex);
                if (!CanSpawnActorBlueprint(actorBlueprint))
                {
                    if(_pendingSummons == null) _pendingSummons = new Queue<ActorSummonData>();
                    _pendingSummons.Enqueue(new ActorSummonData()
                    {
                        Order = i,
                        ActorBlueprint = actorBlueprint,
                        SummonerIndex = nextEntry.SpawnerIndex,
                    });
                }
                else
                {
                    int actorLevel = GetCurrentWaveData().WaveActorsLevel;
                    SpawnActor(actorBlueprint, nextEntry.SpawnerIndex, actorLevel, i);
                }
            }
            _lastSpawnTime = Time.time;

        }
        
        private struct ActorSummonData
        {
            public int SummonerIndex;
            public ActorBlueprint ActorBlueprint;
            public int Order;
        }
        
        /// <summary>
        /// Spawns the actor blueprint
        /// </summary>
        /// <param name="actorBlueprint"></param>
        private Actor SpawnActor(ActorBlueprint actorBlueprint, int summonerIndex, int actorLevel, int order=0)
        {
            if (actorBlueprint == null) return null;
            ActorSummoner summoner = GetSummoner(summonerIndex);
            Actor spawned = summoner.SpawnActor(actorBlueprint, order);
            StatsModule sm = spawned.GetModule<StatsModule>();
            sm.SetLevel(actorLevel);
            
            if (spawned == null) return null;
            OnEnemySpawned?.Invoke(spawned);
            _lastSpawnTime = Time.time;
            return spawned;
        }
        public bool CanSpawnEnemy(WaveEntry waveEntry)
        {
            if (waveEntry.SpawnableIndex < 0) return false;
            if(Time.time - _lastSpawnTime < GetCurrentWaveData().WaveSpawnDelay)
            {
                return false;
            }

            return CanSpawnActorBlueprint(GetActorTemplate(waveEntry.SpawnableIndex));
        }

        public bool CanSpawnActorBlueprint(ActorBlueprint actorBlueprint)
        {
            if (_unitManager != null)
            {
                return _unitManager.CanSpawnActor(actorBlueprint);
            }

            return true;
        }
        public ActorSummoner GetSummoner(int index)
        {
            if (Helpers.IsNullOrEmpty(ActorSummoners))
            {
                return null;
            }

            index = index % ActorSummoners.Count;
            return ActorSummoners[index];
        }
        public ActorBlueprint GetActorTemplate(int index)
        {
            return SpawnablesCollection.GetActorTemplate(index);
        }
        #endregion
        
        #region Wave Information
        public WaveData GetCurrentWaveData()
        {
            if (WaveDatas.IsValidIndex(CurrentWaveIndex))
            {
                return WaveDatas[CurrentWaveIndex];
            }
            return null;
        }
        
        public int GetRemainingEnemyCount(int enemyFactionId)
        {
            int currentlyAlive = _unitManager.GetSpawnedActorCountByFaction(enemyFactionId);
            if (Helpers.IsNullOrEmpty(_waveQueue)) return currentlyAlive;
            foreach (var entry in _waveQueue)
            {
                currentlyAlive += entry.Amount;
            }
            return currentlyAlive;
        }

        public int GetMaxEnemyCountForWave(int waveIndex)
        {
            WaveData waveData = GetWaveDataForWave(waveIndex);
            return waveData.GetEnemyCount();
        }

        public int GetMaxEnemyCount()
        {
            return GetMaxEnemyCountForWave(CurrentWaveIndex);
        }

        public WaveEntry GetNextWaveEntry()
        {
            if (Helpers.IsNullOrEmpty(_waveQueue))
            {
                return new WaveEntry()
                {
                    SpawnableIndex = -1,
                };
            }

            return _waveQueue.Dequeue();
        }

        public WaveEntry PeekNextWaveEntry()
        {
            if (Helpers.IsNullOrEmpty(_waveQueue))
            {
                return new WaveEntry()
                {
                    SpawnableIndex = -1,
                };
            }

            return _waveQueue.Peek();
        }
        #endregion
        
        #region Wave Completion

        private IEnumerator _waveCompleteRoutine = null;
        public void CompleteWave()
        {
            if (_waveCompleteRoutine != null) return;
            if (this == null || ParentLevel == null)
            {
                return;
            }
            StopWave();
            _waveCompleteRoutine = CompleteWaveRoutine();
            StartCoroutine(_waveCompleteRoutine);
        }

        private IEnumerator CompleteWaveRoutine()
        {
            yield return new WaitForSeconds(WaveCompletedDelay);
            OnWaveCompleted?.Invoke();
            _waveCompleteRoutine = null;
        }
        #endregion

        #region Event
        private void OnActorRemoved(Actor removedActor)
        {
            WavePhase wavePhase = ParentLevel.GetCurrentPhase() as WavePhase;
            if (wavePhase == null) return;

            if (removedActor.GetFactionId() == EnemyFactionId)
            {
                _currentMaxUnitsFactor += MaxUnitsFactorIncreasePerKill;
                _currentMaxUnitsFactor = Mathf.Clamp(_currentMaxUnitsFactor, 0f, 1f);
                _unitManager.SetMaxUnitFactorPerFaction(EnemyFactionId, _currentMaxUnitsFactor);
            }
            
            bool waveCompleted = IsWaveCompleted();
            if (waveCompleted)
            {
                CompleteWave();
            }
        }
        #endregion
        
        #region Debug
        [Button("Generate Wave Datas")]
        public void GenerateWaves(WaveGeneratorConfig config, int difficultyLevel, int waveCount)
        {
            //Generate
            WaveDatas = WaveGenerator.Generate(config, SpawnablesCollection, difficultyLevel, waveCount);
            foreach (var wave in WaveDatas)
            {
                Debug.Log("===Wave===");
                foreach (var entry in wave.WaveEntries)
                {
                    Debug.Log($"Entry: SpawnableIndex={entry.SpawnableIndex}, SpawnerIndex={entry.SpawnerIndex}, Amount={entry.Amount}");
                }
            }
        }
        #endregion
    }
}