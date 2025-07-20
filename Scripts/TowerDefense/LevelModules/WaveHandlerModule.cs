using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.RealTimeStrategy;
using Kuantech.Rpg;
using Kuantech.Utils;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.TowerDefense
{
    public class WaveHandlerModule : LevelModule
    {   
        [Header("Properties")]
        public int EnemyFactionId = 1;
        public float WaveCompletedDelay = 0.5f;
        public bool UseLevelPowerAsActorLevel = true;
        
        [Header("Summoners")]
        public List<ActorSummoner> ActorSummoners;
        public SpawnablesCollection SpawnablesCollection;
        
        [Header("Wave Data")]
        public List<WaveData> WaveDatas;
        
        //Runtime
        [NonSerialized] public int CurrentWaveIndex;
        private UnitsManager _unitManager;
        private Queue<WaveEntry> _waveQueue;
        private float _lastSpawnTime;
        
        //Events
        public UnityAction<Actor> OnEnemySpawned;
        public UnityAction OnWaveCompleted;
        
        public override void Initialize()
        {
            base.Initialize();
            if (ActorSummoners.IsNullOrEmpty())
            {
                Debug.LogError("No Actor Summoners assigned to WaveHandlerModule!");
            }
            if (SpawnablesCollection == null)
            {
                Debug.LogError("No Spawnables Collection assigned to WaveHandlerModule!");
            }
            if (WaveDatas.IsNullOrEmpty())
            {
                Debug.LogError("No Wave Data assigned to WaveHandlerModule!");
            }
            CurrentWaveIndex = -1;
        }
        
        public override void PostLevelSetup()
        {
            base.PostLevelSetup();
            _unitManager = ParentLevel.GetLevelModule<UnitsManager>();
            _unitManager.OnActorRemoved += OnActorRemoved;
        }

        public override void OnReset()
        {
            base.OnReset();
            CurrentWaveIndex = -1;
            _waveCompleteRoutine = null;
        }
        
        private void Update()
        {
            if (ParentLevel.CurrentState != LevelState.Playing) return;
            if (IsLevelInWavePhase())
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
            int remainingEnemies = GetRemainingEnemyCount();
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
    
        public void SetNextWave()
        {
            SetWave(CurrentWaveIndex+1);
        }

                
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
            
            for(int i=0;i<waveData.GeneratedEnemyCount; i++)
            {
                int spawnableIndex = waveData.EnemyProbabilities.Sample();
                _waveQueue.Enqueue(new WaveEntry
                {
                    SpawnableIndex = spawnableIndex,
                    SpawnerIndex = -1 //-1 means random spawner
                });
            }
            
            //Set actor limits
            UnitsManager um = ParentLevel.GetLevelModule<UnitsManager>();
            if (um != null)
            {
                um.SetMaxUnitPerFaction(waveData.EnemyFactionId, waveData.MaxEnemyCount);
            }

            _waveCompleteRoutine = null;
        }

        #endregion
        
        #region Summoners
 
        public void SpawnNextWaveElement()
        {
            WaveEntry nextEntry = PeekNextWaveEntry();
            if (!CanSpawnEnemy(nextEntry)) return;
            if (nextEntry.SpawnableIndex < 0) return;

            nextEntry = GetNextWaveEntry(); // Can summon, now pop from queue
            ActorSummoner summoner = GetSummoner(nextEntry.SpawnerIndex);
            ActorBlueprint actorBlueprint = GetActorTemplate(nextEntry.SpawnableIndex);
            if (actorBlueprint == null) return;
            Actor spawned = summoner.SpawnActor(actorBlueprint);
            StatsModule sm = spawned.GetModule<StatsModule>();
            if (sm != null && UseLevelPowerAsActorLevel)
            {
                sm.SetLevel(ParentLevel.GetPowerLevel());
            }
            
            if (spawned == null) return;
            _lastSpawnTime = Time.time;
            OnEnemySpawned?.Invoke(spawned);
        }

        public bool CanSpawnEnemy(WaveEntry waveEntry)
        {
            if (waveEntry.SpawnableIndex < 0) return false;
            if(Time.time - _lastSpawnTime < GetCurrentWaveData().WaveSpawnDelay)
            {
                return false;
            }

            if (_unitManager != null)
            {
                return _unitManager.CanSpawnActor(GetActorTemplate(waveEntry.SpawnableIndex));
            }

            return true;
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
        
        public int GetRemainingEnemyCount()
        {
            if (_waveQueue.IsNullOrEmpty()) return 0;
            return _waveQueue.Count;
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
            if (_waveQueue.IsNullOrEmpty())
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
            if (_waveQueue.IsNullOrEmpty())
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
        private void OnActorRemoved()
        {
            WavePhase wavePhase = ParentLevel.GetCurrentPhase() as WavePhase;
            if (wavePhase == null) return;
            bool waveCompleted = IsWaveCompleted();
            if (waveCompleted)
            {
                CompleteWave();
            }
        }
        #endregion
    }
}