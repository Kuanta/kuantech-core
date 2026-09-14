using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.HordeSurvival
{
    public class HordeWaveHandler : WorldZoneElement
    {
        public enum WaveState
        {
            Waiting,
            Spawning
        }

        [Header("Data")]
        [Tooltip("The enemy roster for this arena. Each blueprint carries its own spawn gating via a " +
                 "HordeEnemyBlueprintComponent, so which ones are eligible is decided per power level.")]
        public ActorBlueprintCollection EnemyBlueprints;
        [Tooltip("The single balance sheet. Budget, cap and enemy level all come from here, scaled by PowerLevel.")]
        public DifficultyConfig Difficulty;

        [Tooltip("Difficulty index for this run — set by the run before the arena starts. Gates the roster, " +
                 "scales budget/cap, and sets the level spawned enemies are created at.")]
        [NonSerialized] public int PowerLevel = 1;

        [Header("Placement")]
        [SerializeReference]
        [Tooltip("Strategy that decides where enemies spawn (annulus around the player, predefined zones, ...).")]
        public SpawnScheme SpawnScheme = new AnnulusSpawnScheme();
        [Tooltip("Spread each batch's enemies around the player (independent annulus angles → a 360° surround, " +
                 "the horde-survival feel). Off = cluster the batch at one point (a pack from one direction).")]
        public bool SpreadSpawnsAroundFocus = true;

        [Tooltip("Chooses which side of the player arrivals come from: ahead of where they are running, and " +
                 "into whichever part of the ring has thinned out. Without it, fleeing turns the horde into a " +
                 "single blob trailing behind.")]
        public HordeSpawnDirector SpawnDirector = new HordeSpawnDirector();

        [Tooltip("Seconds of smoothing on the player's travel direction. Too low and the horde swings around " +
                 "with every twitch of the stick; too high and it lags behind a genuine change of course.")]
        public float TravelSmoothing = 0.35f;

        [Header("Completion")]
        [Tooltip("Delay before completing the wave after all enemies are dead (while ragdolls are still flying).")]
        public float WaveCompleteDelay = 1.5f;

        [Tooltip("If true, the next wave starts automatically after NextWaveDelay once a wave completes.")]
        public bool AutoStartNextWave = true;
        [Tooltip("Delay between a wave completing and the next one starting.")]
        public float NextWaveDelay = 3f;

        [Header("Wave Events")]
        [Tooltip("Scripted moments inside a wave (drop a boss at half budget, a surge at a threshold, ...). " +
                 "Each is a trigger + action; authored here for now, data-driven later.")]
        public List<WaveEvent> WaveEvents = new List<WaveEvent>();

        [Header("Timing")]
        [Tooltip("Delay after the arena activates before the first wave begins — a breath before the run starts.")]
        public float StartDelay = 1f;
        [Tooltip("Delay after a wave is announced (OnWaveStarted) before enemies actually spawn — a 'get ready' beat.")]
        public float StartWaveDelay = 1f;

        /// <summary>
        /// Provides the spawn center (usually the player position). Falls back to this handler's
        /// own transform if not wired. e.g. handler.SpawnCenterProvider = () => player.transform.position;
        /// </summary>
        public Func<Vector3> SpawnCenterProvider;

        //Events
        public UnityAction<int> OnWaveSet;
        public UnityAction<int> OnWaveStarted;
        public UnityAction<Actor> OnEnemySpawned;
        public UnityAction OnWaveCompleted;
        [Tooltip("Fired when the LAST wave of the level clears — the win condition. The run handler ends the level.")]
        public UnityAction OnAllWavesCompleted;

        //Runtime
        public WaveState State;
        private int _currentWaveIndex;
        public int CurrentWaveIndex => _currentWaveIndex;
        private int _remainingBudget;
        private int _concurrentCap;
        private float _lastSpawnTime;
        private float _clearedTime = -1f;
        private IEnumerator _nextWaveRoutine;
        private IEnumerator _startRunRoutine;
        private IEnumerator _startWaveRoutine;
        private WeightedProbabilityArray<ActorBlueprint> _enemyBlueprints;
        [NonSerialized] public Actor Player;
        private EnemyUnitHandler _unitHandler;

        // Smoothed player travel direction on the XZ plane, measured from how the spawn focus moves. Read
        // this way rather than from a movement module so the handler stays agnostic about how the player
        // is driven (twin stick, pathing, a debug fly cam).
        private Vector3 _travelDirection;
        private Vector3 _lastFocus;
        private bool _hasLastFocus;

        public int RemainingBudget => _remainingBudget;

        public override void Initialize(WorldZone zone)
        {
            base.Initialize(zone);
            // Pull the player from the arena; the zone stays generic and never references this handler.
            if (zone is Arena arena)
            {
                Player = arena.Player;
                SpawnCenterProvider = arena.GetPlayerPosition;
            }
            // Unit tracking lives in its own zone element so turret AI, auto-cast targeting, etc. can
            // reach it without going through the wave/budget brain. A sibling under the same Arena.
            _unitHandler = zone.GetZoneElementByType<EnemyUnitHandler>();
            if (_unitHandler == null)
                Debug.LogError("HordeWaveHandler has no sibling EnemyUnitHandler in its Arena — enemies won't be tracked.");
            _currentWaveIndex = -1;
            State = WaveState.Waiting;
        }

        public override void OnZoneActivated()
        {
            base.OnZoneActivated();

            // Wait StartDelay before the first wave (a breath before the run begins), then set + start it.
            if (_startRunRoutine != null) StopCoroutine(_startRunRoutine);
            _startRunRoutine = StartRunRoutine();
            StartCoroutine(_startRunRoutine);
        }

        private IEnumerator StartRunRoutine()
        {
            if (StartDelay > 0f) yield return new WaitForSeconds(StartDelay);
            _startRunRoutine = null;
            SetWave(0);
            StartWave();
        }

        public override void OnZoneDeactivated()
        {
            base.OnZoneDeactivated();
            StopWave();
        }

        private void Update()
        {
            // Tracked even between waves so the direction is already warm when spawning resumes.
            TrackTravelDirection();

            if (State != WaveState.Spawning) return;
            TrySpawnBatch();
            EvaluateWaveEvents();
            CheckWaveCompletion();
        }

        /// <summary>
        /// Measures which way the player is moving by watching the spawn focus. Smoothed, because the raw
        /// per-frame delta on a twin stick is far too jittery to aim a spawn arc with.
        /// </summary>
        private void TrackTravelDirection()
        {
            Vector3 focus = GetFocus();

            if (!_hasLastFocus)
            {
                _lastFocus = focus;
                _hasLastFocus = true;
                return;
            }

            Vector3 delta = focus - _lastFocus;
            delta.y = 0f;
            _lastFocus = focus;

            // A teleport (arena reset, relocation of the focus itself) is not travel — ignore the jump
            // rather than letting it slam the direction to something meaningless.
            float sqrDelta = delta.sqrMagnitude;
            if (sqrDelta > 25f) return;

            // Standing still decays the direction toward zero, which hands the director a pure "re-spread
            // the ring" job instead of a stale heading from before the player stopped.
            Vector3 target = sqrDelta > 0.000001f ? delta.normalized : Vector3.zero;
            float t = TravelSmoothing > 0f ? 1f - Mathf.Exp(-Time.deltaTime / TravelSmoothing) : 1f;
            _travelDirection = Vector3.Lerp(_travelDirection, target, t);
        }

        private Vector3 GetFocus()
        {
            return SpawnCenterProvider != null ? SpawnCenterProvider.Invoke() : transform.position;
        }

        #region Wave Management
        public void SetNextWave()
        {
            SetWave(_currentWaveIndex + 1);
        }

        public void SetWave(int waveIndex)
        {
            _SetWave(waveIndex);
            OnWaveSet?.Invoke(waveIndex);
        }

        private void _SetWave(int waveIndex)
        {
            _currentWaveIndex = waveIndex;
            ResetWaveEvents(); // new wave: let its one-shot events fire again
            _concurrentCap = GetConcurrentCapForWave(waveIndex);
            _remainingBudget = GetWaveBudget(waveIndex);
            _lastSpawnTime = Time.time;

            if (Difficulty == null)
            {
                Debug.LogError("HordeWaveHandler has no DifficultyConfig assigned.");
                return;
            }

            // Build the eligible roster for THIS power level, weighted by each enemy's SpawnWeight. Note
            // the gate is PowerLevel (across levels), not waveIndex (within a level) — the within-level
            // ramp is handled by the budget/cap formulas, not by which enemies are allowed.
            _enemyBlueprints = new WeightedProbabilityArray<ActorBlueprint>();
            if (EnemyBlueprints == null || EnemyBlueprints.ActorBlueprints == null)
            {
                Debug.LogError("Enemy blueprint collection is null");
                return;
            }
            foreach (var blueprint in EnemyBlueprints.ActorBlueprints)
            {
                if (blueprint == null) continue;
                HordeEnemyBlueprintComponent gating = blueprint.GetActorBlueprintComponent<HordeEnemyBlueprintComponent>();
                if (gating == null)
                {
                    Debug.LogWarning($"Enemy blueprint '{blueprint.GetId()}' has no HordeEnemyBlueprintComponent — it can never spawn.");
                    continue;
                }
                if (gating.IsEligibleAt(PowerLevel, waveIndex))
                    _enemyBlueprints.AddElement(blueprint, gating.SpawnWeight);
            }

            if (_enemyBlueprints.IsNullOrEmpty())
            {
                Debug.LogWarning($"No enemies eligible at power level {PowerLevel} (wave {waveIndex}).");
            }
        }

        public void StartWave()
        {
            _clearedTime = -1f;
            // Announce the wave now (banner/UI), but hold off spawning for StartWaveDelay — a 'get ready' beat.
            OnWaveStarted?.Invoke(_currentWaveIndex);

            if (_startWaveRoutine != null) StopCoroutine(_startWaveRoutine);
            _startWaveRoutine = BeginSpawningRoutine();
            StartCoroutine(_startWaveRoutine);
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
            // Cancel every pending timer so a stopped/restarted system doesn't kick off a wave or spawning.
            CancelRoutine(ref _nextWaveRoutine);
            CancelRoutine(ref _startWaveRoutine);
            CancelRoutine(ref _startRunRoutine);
        }

        private void CancelRoutine(ref IEnumerator routine)
        {
            if (routine == null) return;
            StopCoroutine(routine);
            routine = null;
        }

        /// <summary>Maximum enemies alive at once for this wave — from the difficulty sheet, power-scaled.</summary>
        public int GetConcurrentCapForWave(int wave)
        {
            return Difficulty != null ? Difficulty.GetConcurrentCap(wave, PowerLevel) : 1;
        }

        /// <summary>Total threat budget for the wave — from the difficulty sheet, power-scaled.</summary>
        public int GetWaveBudget(int wave)
        {
            return Difficulty != null ? Difficulty.GetWaveBudget(wave, PowerLevel) : 0;
        }
        #endregion

        #region Spawning
        private void TrySpawnBatch()
        {
            if (Difficulty == null) return;
            if (Time.time - _lastSpawnTime < Difficulty.BaseConfig.SpawnInterval) return;
            if (_remainingBudget <= 0) return;
            if (_enemyBlueprints.IsNullOrEmpty()) return;
            if (_unitHandler != null && _unitHandler.AliveCount >= _concurrentCap) return; // cap counts alive enemies only, not corpses

            Vector3 focus = GetFocus();

            // Cluster (pack) mode needs a single anchor for the whole batch; spread mode samples per member,
            // so it only needs the focus. Bail if the cluster anchor can't be found.
            Vector3 batchCenter = focus;
            if (!SpreadSpawnsAroundFocus &&
                (SpawnScheme == null || !SpawnScheme.TryGetSpawnPoint(focus, GetSpawnHint(focus), out batchCenter)))
                return;

            int batchSize = Mathf.Max(1, UnityEngine.Random.Range(Difficulty.BaseConfig.MinBatchSize, Difficulty.BaseConfig.MaxBatchSize + 1));

            for (int i = 0; i < batchSize; i++)
            {
                if (_remainingBudget <= 0) break;
                if (_unitHandler != null && _unitHandler.AliveCount >= _concurrentCap) break;

                ActorBlueprint blueprint = _enemyBlueprints.Sample();
                if (blueprint == null) continue;

                // Spread: each enemy gets its own annulus angle → the batch surrounds the player (360°).
                // Cluster: members huddle around the one anchor → a pack arriving from a single direction.
                Vector3 pos;
                if (SpreadSpawnsAroundFocus)
                {
                    // Hint is re-asked per member: the director picks among the equally-good sectors at
                    // random, so a batch fans out across the whole good arc rather than stacking on one spot.
                    if (SpawnScheme == null || !SpawnScheme.TryGetSpawnPoint(focus, GetSpawnHint(focus), out pos)) continue;
                }
                else
                {
                    pos = GetMemberPosition(batchCenter);
                }

                // Budget is a wave concept, so it is spent here (not inside the shared SpawnEnemy chokepoint),
                // and only when the spawn actually succeeds. Cost comes from the enemy's own gating.
                if (SpawnEnemy(blueprint, pos) != null)
                {
                    HordeEnemyBlueprintComponent gating = blueprint.GetActorBlueprintComponent<HordeEnemyBlueprintComponent>();
                    _remainingBudget -= Mathf.Max(1, gating != null ? gating.SpawnBudget : 1);
                }
            }

            _lastSpawnTime = Time.time;
        }

        /// <summary>
        /// Spreads a batch member around the (already validated) anchor, retrying a few times to
        /// avoid obstacles. Falls back to the anchor itself, which is guaranteed clear.
        /// </summary>
        private Vector3 GetMemberPosition(Vector3 anchor)
        {
            const int attempts = 5;
            for (int i = 0; i < attempts; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * Difficulty.BaseConfig.BatchRadius;
                Vector3 candidate = anchor + new Vector3(offset.x, 0f, offset.y);
                if (SpawnScheme.IsPointValid(candidate)) return candidate;
            }
            return anchor;
        }

        /// <summary>
        /// The single chokepoint for bringing a horde enemy into the world. Every spawner — the wave
        /// loop, the future worker population controller, debug tools — must go through here so that
        /// tracking, lifecycle events, and player wiring all happen in exactly one place.
        /// Deliberately free of wave-specific concerns (budget) so non-wave spawners can reuse it.
        /// </summary>
        public Actor SpawnEnemy(ActorBlueprint blueprint, Vector3 position)
        {
            if (blueprint == null) return null;

            Actor actor = blueprint.CreateActor();
            if (actor == null) return null;

            // Placed before Spawn, not after: Spawn resets the modules and fires OnSpawnedEvent, and anything
            // that reads the transform at that moment (spawn effects, ground snapping, the physics body's
            // first sync) would otherwise see wherever this pooled actor happened to die last.
            actor.transform.position = position;
            actor.Spawn();

            // Scale the enemy to the run's power level: same enemy, level-appropriate stats (HP, damage
            // via each attribute's ValuePerLevel). This is the "quality" axis of difficulty.
            Rpg.StatsModule stats = actor.GetModule<Rpg.StatsModule>();
            if (stats != null && Difficulty != null) stats.SetLevel(Difficulty.GetEnemyLevel(PowerLevel));

            _unitHandler?.RegisterEnemy(actor);

            HordeEnemyModule hem = actor.GetModule<HordeEnemyModule>();
            if (hem == null)
                Debug.LogWarning("Horde enemies must have a HordeEnemyModule");
            else
            {
                if (Player != null) hem.SetPlayer(Player);
                // Own this enemy's recycling: when it drifts too far, hand it a fresh spawn position near the
                // player. Idempotent (-=/+=) so pooled reuse doesn't stack subscriptions.
                hem.OnRelocationRequested -= HandleRelocationRequested;
                hem.OnRelocationRequested += HandleRelocationRequested;
                hem.OnSpawn(position);
            }

            OnEnemySpawned?.Invoke(actor);
            return actor;
        }

        // A drifted-off enemy asks to be recycled: give it a fresh, obstacle-free position near the player.
        // The enemy stays in the alive set the whole time (just moved), so wave counting is untouched.
        private void HandleRelocationRequested(HordeEnemyModule enemy)
        {
            if (enemy == null) return;
            enemy.Relocate(GetEventSpawnPosition());
        }

        #endregion

        #region Wave Completion
        private void CheckWaveCompletion()
        {
            // Wave "cleared" = spawn budget is spent AND no alive enemies remain.
            // Ragdoll corpses (present but not alive) do not block the wave.
            bool cleared = _remainingBudget <= 0 && (_unitHandler == null || _unitHandler.AliveCount == 0);
            if (!cleared)
            {
                _clearedTime = -1f; // an enemy is alive again, reset the timer
                return;
            }

            if (_clearedTime < 0f) _clearedTime = Time.time; // start the grace timer
            if (Time.time - _clearedTime >= WaveCompleteDelay)
                CompleteWave();
        }

        private void CompleteWave()
        {
            State = WaveState.Waiting;
            _clearedTime = -1f;
            OnWaveCompleted?.Invoke();

            // Last wave of the level? That is the win condition — announce it and stop, rather than
            // auto-advancing. WavesPerLevel is 1-based, wave index is 0-based, so the last index is N-1.
            int wavesPerLevel = Difficulty != null ? Mathf.Max(1, Difficulty.WavesPerLevel) : 1;
            if (_currentWaveIndex >= wavesPerLevel - 1)
            {
                OnAllWavesCompleted?.Invoke();
                return;
            }

            if (AutoStartNextWave)
                StartNextWaveDelayed();
        }

        /// <summary>
        /// Schedules the next wave after <see cref="NextWaveDelay"/>. Restarts the timer if already pending.
        /// </summary>
        private void StartNextWaveDelayed()
        {
            if (_nextWaveRoutine != null)
            {
                StopCoroutine(_nextWaveRoutine);
                _nextWaveRoutine = null;
            }
            _nextWaveRoutine = NextWaveRoutine();
            StartCoroutine(_nextWaveRoutine);
        }

        private IEnumerator NextWaveRoutine()
        {
            yield return new WaitForSeconds(NextWaveDelay);
            _nextWaveRoutine = null;
            SetNextWave();
            StartWave();
        }
        #endregion

        #region Wave Events
        // Fires any wave event whose trigger is met. Actions spawn through SpawnEnemy, which lands them in the
        // alive set — so a boss spawned here automatically gates wave completion until it dies.
        private void EvaluateWaveEvents()
        {
            if (WaveEvents == null || WaveEvents.Count == 0) return;

            int lastWave = (Difficulty != null ? Mathf.Max(1, Difficulty.WavesPerLevel) : 1) - 1;
            foreach (var waveEvent in WaveEvents)
            {
                if (waveEvent == null || waveEvent.Trigger == null || waveEvent.Action == null) continue;
                if (waveEvent.OneShot && waveEvent.Fired) continue;

                if (waveEvent.LastWaveOnly)
                {
                    if (_currentWaveIndex != lastWave) continue;
                }
                else if (waveEvent.WaveIndex >= 0 && waveEvent.WaveIndex != _currentWaveIndex)
                {
                    continue;
                }

                if (!waveEvent.Trigger.IsMet(this)) continue;

                waveEvent.Action.Execute(this);
                waveEvent.Fired = true;
            }
        }

        private void ResetWaveEvents()
        {
            if (WaveEvents == null) return;
            foreach (var waveEvent in WaveEvents)
                if (waveEvent != null) waveEvent.Fired = false;
        }

        /// <summary>
        /// A spawn position for wave-event spawns (bosses, surges): a point from the spawn scheme around the
        /// player, falling back to the spawn center. Intentionally bypasses the batch loop's budget and cap.
        /// </summary>
        public Vector3 GetEventSpawnPosition()
        {
            Vector3 focus = GetFocus();
            if (SpawnScheme != null && SpawnScheme.TryGetSpawnPoint(focus, GetSpawnHint(focus), out Vector3 point))
                return point;
            return focus;
        }

        /// <summary>
        /// Which way the next arrival should come from. Everything that puts an enemy near the player goes
        /// through here — the batch loop, relocation, wave events — so they all pull in the same direction
        /// instead of one of them quietly undoing the others.
        /// </summary>
        private SpawnHint GetSpawnHint(Vector3 focus)
        {
            if (SpawnDirector == null) return SpawnHint.None;
            return SpawnDirector.GetHint(focus, _travelDirection, _unitHandler != null ? _unitHandler.AliveEnemies : Array.Empty<Actor>());
        }
        #endregion
    }
}
