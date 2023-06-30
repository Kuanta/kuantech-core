using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class RunnerLevel : Level
    {
        [Header("Runner")]
        [SerializeField] private Transform RunnerStartPoint;
        
        [Header("Chunks")]
        [SerializeField] private GameObject StarterChunkPrefab;
        [SerializeField] private GameObject BaseChunkPrefab;
        [SerializeField] private int LiveChunkCount = 2;
        [SerializeField] private float RetireChunkDelay = 2f;

        private Queue<RunnerChunk> _liveChunks = new Queue<RunnerChunk>();
        private int _currentChunkIndex;
        private RunnerChunk _lastAddedChunk;
        private Runner _currentRunner;
        private RunnerChunk _startChunk = null;
        private LevelFormat _levelFormat;

        private RunnerLevelManager _runnerLevelManager;
        
        public void SetLevelDesign(LevelFormat levelFormat)
        {
            _levelFormat = levelFormat;
        }
        
        public void SetRunner(Runner runner)
        {
            _currentRunner = runner;
            PositionRunner();
        }

        public override void OnLevelCreated()
        {
            base.OnLevelCreated();
            _runnerLevelManager = ((HCGameManager)HCGameManager.Instance).GetSubManagerByType<RunnerLevelManager>() as RunnerLevelManager;
        }
        public override void PrepareLevel()
        {
            ClearLevel();
            PositionRunner();
            for (int i = 0; i < LiveChunkCount; ++i)
            {
                CreateAndAttachChunk();
            }
        }

        private void PositionRunner()
        {
            if (_currentRunner == null) return;
            Transform runnerTransform = _currentRunner.transform;
            if (RunnerStartPoint != null)
            {
                runnerTransform.position = RunnerStartPoint.position;
                runnerTransform.rotation = RunnerStartPoint.rotation;
            }
            else
            {
                runnerTransform.localPosition = Vector3.zero;
                runnerTransform.localRotation = Quaternion.identity;
            }
        }
        public override void ClearLevel()
        {
            StopAllCoroutines();
            ReleaseEarnings();
            if (_liveChunks == null) return;
            foreach (var chunk in _liveChunks)
            {
                RetireChunk(chunk);
            }
            _liveChunks.Clear();
            _lastAddedChunk = null;
            _currentChunkIndex = 0;
        }
        public void OnPlayerExitChunk(RunnerChunk chunk)
        {
            if (CurrentState != LevelState.Playing) return;
            
            //Add a chunk, remove the oldest one
            Debug.LogError($"Left Chunk {chunk.gameObject.name}");
            if (chunk == _startChunk) return;
            _startChunk = null; //No needed anymore
            RunnerChunk newChunk = CreateAndAttachChunk();
            if (newChunk == null) return; //No new chunk has been added
            RunnerChunk oldestChunk = _liveChunks.Dequeue();
            RetireChunk(oldestChunk);
        }
        
        #region ChunkGeneration
        public void AttachChunk(RunnerChunk newChunk)
        {
            newChunk.transform.SetParent(transform);
            _liveChunks.Enqueue(newChunk);
            if (_lastAddedChunk == null)
            {
                newChunk.transform.localPosition = Vector3.zero;
                newChunk.transform.localRotation = Quaternion.identity;
            }
            else
            {
                _lastAddedChunk.AttachNewChunk(newChunk);    
            }
            _lastAddedChunk = newChunk;
        }

        public RunnerChunk CreateAndAttachChunk()
        {
            if (_currentChunkIndex >= _levelFormat.Chunks.Count)
            {
                //todo: Consider endless runner here
                Debug.LogError("Already at the end, can't generate more chunks");
                return null;
            }

            bool isFinal = _currentChunkIndex == _levelFormat.Chunks.Count - 1;
            ChunkFormat chunkFormat = _levelFormat.Chunks[_currentChunkIndex];
            Debug.LogError($"Getting {chunkFormat.ChunkType.ToString()}");
            GameObject chunkPrefab = _runnerLevelManager.GetChunkPrefab(chunkFormat.ChunkType);
            GameObject chunk = Instantiate(chunkPrefab);//GameManager.Instance.Pool.GetObject(prefab);
            RunnerChunk runnerChunk = chunk.GetComponent<RunnerChunk>();
            AttachChunk(runnerChunk);
            
            //Position of the chunk must be set before initialize. (Spawning objects need correct positions)
            runnerChunk.Initialize(this, chunkFormat, isFinal);
            _currentChunkIndex++;
            return runnerChunk;
        }

        private void RetireChunk(RunnerChunk chunk)
        {
            chunk.ClearChunk();
            //GameManager.Instance.Pool.PoolObject(chunk.gameObject);
            Destroy(chunk.gameObject);
            //StartCoroutine(RetireChunkCoroutine(chunk, 0));
        }

        private IEnumerator RetireChunkCoroutine(RunnerChunk chunk, float delay)
        {
            yield return new WaitForSeconds(delay);
            chunk.ClearChunk();
            //GameManager.Instance.Pool.PoolObject(chunk.gameObject);
            Destroy(chunk.gameObject);
        }
        #endregion
    }
}