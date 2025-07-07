using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.RealTimeStrategy;
using Kuantech.Utils;
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
        }
        
        private void Update()
        {
            if (ParentLevel.CurrentState != LevelState.Playing) return;
            if (IsLevelInWavePhase())
            {
                SpawnNextWaveElement();
            }
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
        
        public bool IsLevelInWavePhase()
        {
            return ParentLevel.GetCurrentPhase() is WavePhase;
        }

        #region Wave Control
        public bool AreAllWavesCompleted()
        {
            return CurrentWaveIndex >= WaveDatas.Count - 1;
        }
        
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
        }

        #endregion
        #region Summoners
 
        public void SpawnNextWaveElement()
        {
            if(Time.time - _lastSpawnTime < GetCurrentWaveData().WaveSpawnDelay)
            {
                return;
            }

            WaveEntry nextEntry = GetNextWaveEntry();
            if (nextEntry.SpawnableIndex < 0) return;

            ActorSummoner summoner = GetSummoner(nextEntry.SpawnerIndex);
            ActorBlueprint actorBlueprint = GetActorTemplate(nextEntry.SpawnableIndex);
            if (actorBlueprint == null) return;
            Actor spawned = summoner.SpawnActor(actorBlueprint);
            if (spawned == null) return;
            _lastSpawnTime = Time.time;
            OnEnemySpawned?.Invoke(spawned);
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

        public int GetMaxEnemyCount()
        {
            WaveData waveData = GetCurrentWaveData();
            if (waveData == null) return 0;
            return waveData.GetEnemyCount();    
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
        #endregion

        
        #region Wave Completion

        public void CompleteWave()
        {
            StartCoroutine(CompleteWaveRoutine());
        }

        private IEnumerator CompleteWaveRoutine()
        {
            yield return new WaitForSeconds(WaveCompletedDelay);
            OnWaveCompleted?.Invoke();
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