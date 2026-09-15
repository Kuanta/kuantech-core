using System.Collections;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Networking;
#if NETWORKING_NGO
using Unity.Netcode;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// Server-authoritative horde wave spawner, built from scratch for NGO rather than reusing
    /// HordeWaveHandler as-is -- that one creates enemies through a local, non-networked
    /// ActorBlueprint/PoolManager pipeline and assumes a single player. This one spawns real
    /// NetworkObjects (server-only) and picks its spawn focus from however many players are currently
    /// connected. Shares the same budget/cap/scheme shape and DifficultyConfig math, and hands every
    /// spawned enemy to a sibling EnemyUnitHandler exactly like the original did.
    ///
    /// Waves auto-continue forever by design (no WavesPerLevel win condition) -- budget/cap keep scaling
    /// with wave index via DifficultyConfig, tune BudgetQuad/CapPerWave to keep that in check long-term.
    /// </summary>
    public class NetworkedWaveHandler : WorldZoneElement
    {
        public enum WaveState
        {
            Waiting,
            Spawning
        }

        [Header("Enemy")]
        [Tooltip("NetworkObject prefab to spawn. Must be registered in NetworkManager's Network Prefabs list.")]
#if NETWORKING_NGO
        public NetworkObject EnemyPrefab;
#endif
        [Tooltip("FactionHandler.BelongingFaction value used to find players for spawn-focus purposes.")]
        public int PlayerFaction = 0;

        [Header("Data")]
        [Tooltip("The single balance sheet. Budget, cap and enemy level all come from here, scaled by PowerLevel.")]
        public DifficultyConfig Difficulty;
        [Tooltip("Difficulty index for this run. Gates nothing here (single enemy type) but still scales " +
                 "budget/cap/enemy level via DifficultyConfig.")]
        public int PowerLevel = 1;

        [Header("Placement")]
        [SerializeReference]
        [Tooltip("Strategy that decides where enemies spawn -- defaults to fixed points (spawn gates).")]
        public SpawnScheme SpawnScheme = new FixedPointSpawnScheme();
        [Tooltip("Spread each batch's enemies around the focus independently (a 360° surround) vs. " +
                 "clustering the whole batch at one anchor (a pack from one direction).")]
        public bool SpreadSpawnsAroundFocus = true;

        [Header("Completion")]
        [Tooltip("Delay before completing the wave after all enemies are dead (while ragdolls are still flying).")]
        public float WaveCompleteDelay = 1.5f;
        [Tooltip("If true, the next wave starts automatically after NextWaveDelay once a wave completes.")]
        public bool AutoStartNextWave = true;
        public float NextWaveDelay = 3f;

        [Header("Timing")]
        [Tooltip("Delay after the zone activates before the first wave begins.")]
        public float StartDelay = 1f;
        [Tooltip("Delay after a wave is announced (OnWaveStarted) before enemies actually start spawning.")]
        public float StartWaveDelay = 1f;

        // Events
        public UnityAction<int> OnWaveSet;
        public UnityAction<int> OnWaveStarted;
        public UnityAction<Actor> OnEnemySpawned;
        public UnityAction OnWaveCompleted;

        // Runtime
        public WaveState State { get; private set; }
        private int _currentWaveIndex;
        public int CurrentWaveIndex => _currentWaveIndex;
        private int _remainingBudget;
        private int _concurrentCap;
        private float _lastSpawnTime;
        private float _clearedTime = -1f;
        private Coroutine _startRunRoutine;
        private Coroutine _startWaveRoutine;
        private Coroutine _nextWaveRoutine;
        private EnemyUnitHandler _unitHandler;

        public int RemainingBudget => _remainingBudget;

        public override void Initialize(WorldZone zone)
        {
            base.Initialize(zone);
            _unitHandler = zone.GetZoneElementByType<EnemyUnitHandler>();
            if (_unitHandler == null)
                Debug.LogError("NetworkedWaveHandler has no sibling EnemyUnitHandler in its zone -- enemies won't be tracked.");
            _currentWaveIndex = -1;
            State = WaveState.Waiting;

#if NETWORKING_NGO
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
#endif
        }

        public override void CleanupZone()
        {
            base.CleanupZone();
#if NETWORKING_NGO
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
#endif
        }

#if NETWORKING_NGO
        // A client that connects after a wave already started never saw its (fire-and-forget) announcement
        // -- catch just that one client up on whatever wave is currently active.
        private void OnClientConnected(ulong clientId)
        {
            if (!KtNetworkManager.HasAuthority() || _currentWaveIndex < 0) return;
            WaveAnnouncer.AnnounceWaveStartedTo(_currentWaveIndex, clientId);
        }
#endif

        public override void OnZoneActivated()
        {
            base.OnZoneActivated();
            if (!KtNetworkManager.HasAuthority()) return;

            if (_startRunRoutine != null) StopCoroutine(_startRunRoutine);
            _startRunRoutine = StartCoroutine(StartRunRoutine());
        }

        public override void OnZoneDeactivated()
        {
            base.OnZoneDeactivated();
            StopWave();
        }

        private IEnumerator StartRunRoutine()
        {
            if (StartDelay > 0f) yield return new WaitForSeconds(StartDelay);
            _startRunRoutine = null;
            SetWave(0);
            StartWave();
        }

        private void Update()
        {
            if (!KtNetworkManager.HasAuthority() || State != WaveState.Spawning) return;
            TrySpawnBatch();
            CheckWaveCompletion();
        }

        #region Wave Management

        public void SetNextWave() => SetWave(_currentWaveIndex + 1);

        public void SetWave(int waveIndex)
        {
            _currentWaveIndex = waveIndex;
            _concurrentCap = GetConcurrentCapForWave(waveIndex);
            _remainingBudget = GetWaveBudget(waveIndex);
            _lastSpawnTime = Time.time;

            if (Difficulty == null)
                Debug.LogError("NetworkedWaveHandler has no DifficultyConfig assigned.");

            OnWaveSet?.Invoke(waveIndex);
        }

        public void StartWave()
        {
            _clearedTime = -1f;
            OnWaveStarted?.Invoke(_currentWaveIndex);
            WaveAnnouncer.AnnounceWaveStarted(_currentWaveIndex);

            if (_startWaveRoutine != null) StopCoroutine(_startWaveRoutine);
            _startWaveRoutine = StartCoroutine(BeginSpawningRoutine());
        }

        private IEnumerator BeginSpawningRoutine()
        {
            if (StartWaveDelay > 0f) yield return new WaitForSeconds(StartWaveDelay);
            _startWaveRoutine = null;
            _lastSpawnTime = Time.time;
            State = WaveState.Spawning;
        }

        public void StopWave()
        {
            State = WaveState.Waiting;
            CancelRoutine(ref _nextWaveRoutine);
            CancelRoutine(ref _startWaveRoutine);
            CancelRoutine(ref _startRunRoutine);
        }

        private void CancelRoutine(ref Coroutine routine)
        {
            if (routine == null) return;
            StopCoroutine(routine);
            routine = null;
        }

        public int GetConcurrentCapForWave(int wave) => Difficulty != null ? Difficulty.GetConcurrentCap(wave, PowerLevel) : 1;
        public int GetWaveBudget(int wave) => Difficulty != null ? Difficulty.GetWaveBudget(wave, PowerLevel) : 0;

        #endregion

        #region Spawning

        private void TrySpawnBatch()
        {
            if (Difficulty == null) return;
            if (Time.time - _lastSpawnTime < Difficulty.BaseConfig.SpawnInterval) return;
            if (_remainingBudget <= 0) return;
            if (_unitHandler != null && _unitHandler.AliveCount >= _concurrentCap) return;

            Vector3 focus = GetFocus();

            Vector3 batchCenter = focus;
            if (!SpreadSpawnsAroundFocus &&
                (SpawnScheme == null || !SpawnScheme.TryGetSpawnPoint(focus, out batchCenter)))
                return;

            int batchSize = Mathf.Max(1, Random.Range(Difficulty.BaseConfig.MinBatchSize, Difficulty.BaseConfig.MaxBatchSize + 1));

            for (int i = 0; i < batchSize; i++)
            {
                if (_remainingBudget <= 0) break;
                if (_unitHandler != null && _unitHandler.AliveCount >= _concurrentCap) break;

                Vector3 pos;
                if (SpreadSpawnsAroundFocus)
                {
                    if (SpawnScheme == null || !SpawnScheme.TryGetSpawnPoint(focus, out pos)) continue;
                }
                else
                {
                    pos = GetMemberPosition(batchCenter);
                }

                if (SpawnEnemy(pos) != null)
                    _remainingBudget -= 1; // single enemy type for now -- no per-enemy budget cost yet
            }

            _lastSpawnTime = Time.time;
        }

        private Vector3 GetMemberPosition(Vector3 anchor)
        {
            const int attempts = 5;
            for (int i = 0; i < attempts; i++)
            {
                Vector2 offset = Random.insideUnitCircle * Difficulty.BaseConfig.BatchRadius;
                Vector3 candidate = anchor + new Vector3(offset.x, 0f, offset.y);
                if (SpawnScheme.IsPointValid(candidate)) return candidate;
            }
            return anchor;
        }

        /// <summary>
        /// The single chokepoint for bringing a horde enemy into the world -- server-only NetworkObject
        /// spawn, then handed to EnemyUnitHandler for tracking. The spawned Actor's own EnemyModule drives
        /// itself from there (TargetDetectionModule already finds the nearest player on its own), so unlike
        /// HordeWaveHandler this needs no per-enemy "SetPlayer" wiring.
        /// </summary>
        public Actor SpawnEnemy(Vector3 position)
        {
#if NETWORKING_NGO
            if (EnemyPrefab == null) return null;
            NetworkObject instance = Instantiate(EnemyPrefab, position, Quaternion.identity);
            instance.Spawn();

            Actor actor = instance.GetComponent<Actor>();
            if (actor == null) return null;

            Rpg.StatsModule stats = actor.GetModule<Rpg.StatsModule>();
            if (stats != null && Difficulty != null) stats.SetLevel(Difficulty.GetEnemyLevel(PowerLevel));

            _unitHandler?.RegisterEnemy(actor);
            OnEnemySpawned?.Invoke(actor);
            return actor;
#else
            return null;
#endif
        }

        #endregion

        #region Wave Completion

        private void CheckWaveCompletion()
        {
            bool cleared = _remainingBudget <= 0 && (_unitHandler == null || _unitHandler.AliveCount == 0);
            if (!cleared)
            {
                _clearedTime = -1f;
                return;
            }

            if (_clearedTime < 0f) _clearedTime = Time.time;
            if (Time.time - _clearedTime >= WaveCompleteDelay)
                CompleteWave();
        }

        private void CompleteWave()
        {
            State = WaveState.Waiting;
            _clearedTime = -1f;
            OnWaveCompleted?.Invoke();

            if (AutoStartNextWave)
                StartNextWaveDelayed();
        }

        private void StartNextWaveDelayed()
        {
            if (_nextWaveRoutine != null)
            {
                StopCoroutine(_nextWaveRoutine);
                _nextWaveRoutine = null;
            }
            _nextWaveRoutine = StartCoroutine(NextWaveRoutine());
        }

        private IEnumerator NextWaveRoutine()
        {
            yield return new WaitForSeconds(NextWaveDelay);
            _nextWaveRoutine = null;
            SetNextWave();
            StartWave();
        }

        #endregion

        #region Focus (coop-aware)

        /// <summary>
        /// A random currently-alive player's position -- picking a different one per batch spreads spawns
        /// across the map when players split up, instead of every spawn always centering on player 1.
        /// Falls back to this handler's own transform if nobody is found (e.g. between connections).
        /// </summary>
        private Vector3 GetFocus()
        {
            List<Actor> players = GetAlivePlayers();
            if (players.Count == 0) return transform.position;
            return players[Random.Range(0, players.Count)].GetActorLocation();
        }

        private List<Actor> GetAlivePlayers()
        {
            List<Actor> result = new List<Actor>();
            foreach (var actor in ActorManager.GetAllActors())
            {
                if (actor == null || !actor.IsAlive()) continue;
                if (actor.FactionHandler.BelongingFaction != PlayerFaction) continue;
                result.Add(actor);
            }
            return result;
        }

        #endregion
    }
}
