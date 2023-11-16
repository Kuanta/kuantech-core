using System.Collections.Generic;
using System.Linq;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{

    public class RunnerLevel : Level
    {
        private List<GameObject> _availableChunkContents;

        [Header("Properties")] 
        public bool Generated;
        public int ChunkCount;
        
        [Header("Runner")]
        [SerializeField] private Transform RunnerStartPoint;

        [Header("Chunks")] 
        public ChunkSet ChunkSet;
        [SerializeField] private int LiveChunkCount = 4;

        private Queue<RunnerChunk> _liveChunks = new Queue<RunnerChunk>();
        private int _currentChunkIndex;
        protected Runner CurrentRunner;
        private RunnerChunk _startChunk = null;

        private LevelManager _runnerLevelManager;
        
        [Header("Level Limits")]
        public Vector3 CurrentLevelForward = new Vector3(1,0,0);
        public float CurrentLevelWidth;
        public void SetRunner(Runner runner)
        {
            CurrentRunner = runner;
            PositionRunner();
        }
        
        public override void OnLevelCreated()
        {
            base.OnLevelCreated();
            //Get existing chunks
            LevelChunks = GetComponentsInChildren<LevelChunk>().ToList();
            if (LevelChunks.Count == 0) Debug.LogError("Premade level has no chunk");
            _currentChunkIndex = 0;
            _startChunk = LevelChunks[0] as RunnerChunk;
            ChunkCount = LevelChunks.Count;
            for (int i = 0; i < LevelChunks.Count; ++i)
            {
                RunnerChunk rc = LevelChunks[i] as RunnerChunk;
                rc.Initialize(this, rc.IsFinalChunk);
                if (i < LiveChunkCount)
                {
                    rc.gameObject.SetActive(true);
                    _liveChunks.Enqueue(rc);
                }
                else
                {
                    rc.gameObject.SetActive(false);
                }
            }

            RunnerManager runnerMan = GameManager.Instance.GetSubManagerByType<RunnerManager>() as RunnerManager;
            SetRunner(runnerMan.Runner);
        }

        public override void PrepareLevel()
        {
            base.PrepareLevel();
            PositionRunner();
        }
        
        /// <summary>
        /// Restarts the level. If not endless, doesn't clear existing chunks.
        /// </summary>
        public override void RestartLevel()
        {
            base.RestartLevel();
            PositionRunner();
            if (ChunkCount <= 0 && !Generated) //Its endless, remove all
            {
                ClearLevel();
                return;
            }

            _currentChunkIndex = 0;
            _liveChunks.Clear();
            for (int i = 0; i < LevelChunks.Count; ++i)
            {
                LevelChunks[i].gameObject.SetActive(i<LiveChunkCount);
                LevelChunks[i].OnRestart();
                _liveChunks.Enqueue(LevelChunks[i] as RunnerChunk);
                _currentChunkIndex++;
            }

            _startChunk = LevelChunks[0] as RunnerChunk;
        }
        private void PositionRunner()
        {
            if (CurrentRunner == null) return;
            Transform runnerTransform = CurrentRunner.transform;
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
        
        /// <summary>
        /// Clears the level by clearing and destroying all chunks.
        /// </summary>
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
            _currentChunkIndex = 0;
        }

        public void OnPlayerEnterChunk(RunnerChunk chunk)
        {
            
        }
        
        /// <summary>
        /// Called when player exits a chunk
        /// </summary>
        /// <param name="chunk"></param>
        public void OnPlayerExitChunk(RunnerChunk chunk)
        {
            if (CurrentState != LevelState.Playing) return;
            
            if (chunk == _startChunk) return;
            _startChunk = null; //No needed anymore
            RunnerChunk nextChunk = GenerateAndAtttachNextChunk();
            _currentChunkIndex++; //Went to next chunk
            
            if (nextChunk == null) return; //No new chunk has been added
            RunnerChunk oldestChunk = _liveChunks.Dequeue();

            if (ChunkCount <= 0)
            {
                RetireChunk(oldestChunk);
            }
            else
            {
                oldestChunk.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Combines generating and attaching next chunk to a single method
        /// </summary>
        private RunnerChunk GenerateAndAtttachNextChunk()
        {
            RunnerChunk nextChunk = null;
            if (LevelChunks.Count > _currentChunkIndex && LevelChunks[_currentChunkIndex + 1] != null)
            {
                nextChunk = LevelChunks[_currentChunkIndex + 1] as RunnerChunk;
                nextChunk.gameObject.SetActive(true);
                nextChunk.OnRestart();
                return nextChunk;
            }
            nextChunk = GenerateNextChunk();
            if (nextChunk == null) return nextChunk;
            AttachChunk(nextChunk);
            LevelChunks.Add(nextChunk);
            return nextChunk;
        }
        #region Chunks Generation

        
        /// <summary>
        /// Generates a chunk given the chunk type
        /// </summary>
        /// <param name="chunkType"></param>
        /// <returns></returns>
        public RunnerChunk GenerateNextChunk()
        {
            if (ChunkCount > 0 && _currentChunkIndex >= ChunkCount)
            {
                //todo: Consider endless runner here
                return null;
            }
            
            bool isFinal = ChunkCount > 0 && _currentChunkIndex == ChunkCount - 1;
            ChunkType chunkType = ChunkType.Corridor;
            if (_currentChunkIndex == 0)
            {
                chunkType = ChunkType.StartChunk;
            }else if (isFinal)
            {
                chunkType = ChunkType.EndChunk;
            }
            
            //Instantiate base chunk
            GameObject baseChunkPrefab = ChunkSet.GetRandomBaseChunk(chunkType);
            return InstantiateNextChunk(baseChunkPrefab, chunkType, isFinal);
        }

        protected RunnerChunk InstantiateNextChunk(GameObject baseChunkPrefab, ChunkType chunkType, bool isFinal)
        {
            GameObject baseChunk = Instantiate(baseChunkPrefab);
            RunnerChunk runnerChunk = baseChunk.GetComponent<RunnerChunk>();
            
            //Instantiate chunk contents
            if (chunkType != ChunkType.StartChunk && 
                chunkType != ChunkType.EndChunk &&
                chunkType != ChunkType.BossChunk)
            {
                //Chunk contents not available for start and end chunks
                GameObject chunkContentsPrefab = _availableChunkContents.GetRandomElement();
                GameObject chunkContents = Instantiate(chunkContentsPrefab, runnerChunk.transform);
                chunkContents.transform.localPosition = Vector3.zero;
                chunkContents.transform.localRotation = Quaternion.identity;
            }
            runnerChunk.Initialize(this, isFinal);
            return runnerChunk;
        }
        
        /// <summary>
        /// Attaches the freshly created chunk to the level
        /// </summary>
        /// <param name="newChunk"></param>
        public void AttachChunk(RunnerChunk newChunk)
        {
            newChunk.transform.SetParent(transform);
            if(_liveChunks != null) _liveChunks.Enqueue(newChunk);
            RunnerChunk _lastAddedChunk = GetLastAddedChunk();
            if (_lastAddedChunk == null)
            {
                newChunk.transform.localPosition = Vector3.zero;
                newChunk.transform.localRotation = Quaternion.identity;
            }
            else
            {
                _lastAddedChunk.AttachNewChunk(newChunk);    
            }
        }

        private RunnerChunk GetLastAddedChunk()
        {
            if (LevelChunks.Count == 0) return null;
            return LevelChunks[^1] as RunnerChunk;
        }
        
        private void RetireChunk(RunnerChunk chunk)
        {
            chunk.ClearChunk();
            Destroy(chunk.gameObject);
        }

        #endregion

        #region LevelLimits
        /// <summary>
        /// Returns the current forward of the lane
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCurrentForward()
        {
            return CurrentLevelForward;
        }

        public Vector3 GetCurrentRight()
        {
            return Quaternion.AngleAxis(90f, Vector3.up) * CurrentLevelForward; 
        }

        /// <summary>
        /// Returns the center point for lane center
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCurrentLaneCenter()
        {
            return transform.position;
        }

        public float GetCurrentLaneWidth()
        {
            return CurrentLevelWidth;
        }
        #endregion
        
    }
}