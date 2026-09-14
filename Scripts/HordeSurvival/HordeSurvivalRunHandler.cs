using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Core.Controller;
using Kuantech.Core.Store;
using Kuantech.Core.Utils;
using Kuantech.HordeSurvival;
using Kuantech.Midcore;
using Kuantech.RogueLike;
using Kuantech.Rpg;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.HordeSurvival
{
    // /// <summary>
    // /// Input config for a single run, handed over on scene transition (which arena, difficulty, seed, ...).
    // /// </summary>
    // [Serializable]
    // public struct HordeSurvivalRunData
    // {
    //     public string ArenaId;
    //     public int PowerLevel;
    //     // difficulty, seed, loadout, ... later
    // }

    /// <summary>
    /// Live, pure-data state of the current run. Read by UI, potentially serialized for resume. Owns the
    /// perk XP curve (orb-driven) and simple run stats. Not a MonoBehaviour — the handler drives it.
    /// </summary>
    public class HordeSurvivalRunState
    {
        public readonly LevelVariable PerkXp;
        public int Kills;
        public float ElapsedTime;
        public int PendingPerkSelections;
        public bool IsActive;

        /// <summary>
        /// Where the run is in its lifecycle. Systems that must go quiet outside play (enemy brains,
        /// player input) read this rather than each inventing their own idea of "is the run over".
        /// </summary>
        public HordeSurvivalRunHandler.RunPhase Phase = HordeSurvivalRunHandler.RunPhase.Waiting;

        /// <summary>
        /// What this run has earned so far, banked in one go when the run ends. Kept as Rewards rather
        /// than a bare gold counter so the results screen can show anything a run can pay out — and so
        /// currency never touches CurrencyManager mid-run, which would hit the disk on every pickup.
        /// </summary>
        public readonly List<Reward> Rewards = new();

        public HordeSurvivalRunState(LevelVariableData perkXpConfig)
        {
            PerkXp = new LevelVariable(perkXpConfig);
        }
    }

    /// <summary>
    /// Orchestrates one run: spawns the arena and the (single, run-long) player, tracks orb-driven perk
    /// XP, and raises events when a perk should be picked. It coordinates — the wave spawning lives in
    /// HordeWaveHandler, the XP curve in LevelVariable, and perk application will live in a perk system.
    /// Bind the events to UI/systems separately.
    /// </summary>
    public class HordeSurvivalRunHandler : SubManager
    {
        public enum RunPhase
        {
            Waiting,
            Running,
            Ended,
        }

        // [Header("Config")]
        // public HordeSurvivalRunData RunData;
        //public Arena ArenaPrefab;
        public ActorBlueprint PlayerBlueprint;
        [SerializeField] private IsometricCameraFollower CameraFollower;
        public LevelVariableData PerkXpConfig = new LevelVariableData { BaseRequirement = 100f, GrowthFactor = 1.5f };
        public PerkSelectionHandler PerkSelection;
        [Tooltip("How many perk cards to offer on a level-up.")]
        [SerializeField] private int PerkChoiceCount = 3;

        [Header("UI")]
        [SerializeField] private string PerkSelectionPanelId = "PerkSelectionPanel";

        //Runtime
        [NonSerialized] public Arena CurrentArena;
        [NonSerialized] public Actor Player;
        [NonSerialized] public HordeSurvivalRunState RunState;
        [NonSerialized] public ArenaData ArenaData;
        // The campaign coordinates of the current run — kept so completion can mark the right (arena, tier)
        // and a restart can rebuild it from the same source.
        [NonSerialized] public int CurrentArenaIndex;
        [NonSerialized] public int CurrentTier;

        //Events — bind UI/systems to these
        public UnityAction OnRunStarted;
        public UnityAction OnPerkXpChanged;
        public UnityAction OnPerkSelectionAvailable;
        public UnityAction OnRewardsChanged;
        public UnityAction OnRunCompleted;
        public UnityAction OnRunEnded;
        public UnityAction OnRunFailed;
        public UnityAction<RunPhase> OnRunStateChanged;


        #region Run Lifecycle

        public async override UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            PerkSelection.Initialize();
        }


        // Starts the run for a campaign (arena, tier). The arena layout prefab + this tier's difficulty
        // (PowerLevel) are resolved from the ArenaManager (the DontDestroy campaign owner); the run handler
        // only orchestrates the live run in the game scene.
        public void StartRun(int arenaIndex, int tier)
        {
            ArenaManager arenaManager = ArenaManager.GetContext<ArenaManager>();
            ArenaData arenaData = arenaManager != null ? arenaManager.GetArenaData(arenaIndex, tier) : null;
            // The prefab is registered on the ArenaManager (by the Arena component's own ArenaId), not in
            // the generic AssetCollection -- arena identity belongs with the rest of the campaign data.
            Arena arenaPrefab = arenaData != null && arenaManager != null
                ? arenaManager.GetArenaById(arenaData.ArenaPrefabId)
                : null;
            if (arenaData == null || arenaPrefab == null)
            {
                Debug.LogError($"StartRun: could not resolve arena {arenaIndex} tier {tier} (arena data / prefab missing).");
                return;
            }

            CurrentArenaIndex = arenaIndex;
            CurrentTier = tier;
            ArenaData = arenaData;

            RunState = new HordeSurvivalRunState(PerkXpConfig);
            RunState.PerkXp.OnLevelUp += HandlePerkLevelUp;

            CurrentArena = Instantiate(arenaPrefab);
            Player = SpawnPlayer();

            // Feed the animation-LOD system a cheap shared player position so distant enemies throttle their
            // animators. Reads the live Player field each call; cleared on teardown.
            AnimationLodModule.PlayerPositionProvider = () => Player != null ? Player.GetActorLocation() : Vector3.zero;

            CurrentArena.InitializeArena(Player, ArenaData);
            SubscribeToWaveCompletion();
            CurrentArena.StartArena();

            RunState.IsActive = true;
            // After RunState and Player exist: the phase lives on the state and toggles player input.
            SetCurrentPhase(RunPhase.Running);
            OnRunStarted?.Invoke();

        }

        /// <summary>
        /// Ends the run as a success. On top of the gold earned during the run (always banked, even on a
        /// fail), a win pays the tier's Rewards, plus a one-time FirstClearRewards jackpot the
        /// first time this tier is cleared.
        /// </summary>
        public void CompleteRun()
        {
            if (RunState == null || !RunState.IsActive) return; // guard: EndRun must only run once
            ArenaManager arenaManager = ArenaManager.GetContext<ArenaManager>();

            // Ask BEFORE CompleteTier marks it done, so the first-clear jackpot is only paid once.
            bool firstClear = arenaManager == null || !arenaManager.IsTierCompleted(CurrentArenaIndex, CurrentTier);

            // Route win rewards through the run's pending list so they bank uniformly in EndRun and show on
            // the results screen next to the run's gold.
            if (ArenaData != null)
            {
                GrantRewards(ArenaData.Rewards);
                if (firstClear) GrantRewards(ArenaData.FirstClearRewards);
            }

            // Progression: clearing the run clears this (arena, tier) — the ArenaManager opens the next tier
            // (or the next arena once all tiers are done) and advances the selection.
            if (arenaManager != null) arenaManager.CompleteTier(CurrentArenaIndex, CurrentTier);
            OnRunCompleted?.Invoke();
            EndRun();
        }

        // Queues authored rewards into this run's pending list. Currency stacks into the run's totals (one
        // "gold" line) via AddCurrencyReward, which never touches the authored template; other rewards
        // (items, collectibles) are banked as-is and shown as their own results entry.
        private void GrantRewards(List<Reward> rewards)
        {
            if (rewards == null) return;
            foreach (var reward in rewards)
            {
                if (reward == null) continue;
                if (reward is CurrencyReward currency)
                    AddCurrencyReward(CurrencyManager.GetCurrencyAssetById(currency.CurrencyId), currency.GetAmount());
                else
                    AddReward(reward);
            }
        }

        public void FailRun()
        {
            if (RunState == null || !RunState.IsActive) return; // fail once (guard against double-calls)
            OnRunFailed?.Invoke();
            EndRun();
        }
        
        public void EndRun()
        {
            if (RunState == null || !RunState.IsActive) return;
            RunState.IsActive = false;
            // Phase first: it stops enemy brains and player input, so the world freezes before anything
            // else happens. Otherwise enemies keep attacking over the results screen.
            SetCurrentPhase(RunPhase.Ended);
            if (CurrentArena != null) CurrentArena.DeactivateZone(); // stop waves + worker spawning

            // Bank before announcing: whoever listens to OnRunEnded (results screen, menu transition)
            // should see the currency already credited. The IsActive guard above makes this run once.
            EarnRewards();

            OnRunEnded?.Invoke();
        }

        /// <summary>
        /// Full teardown when the game scene is left. Pooled enemies/workers/drops live in the (DontDestroy)
        /// pool, so a scene unload alone won't reclaim them — they must be returned to the pool explicitly or
        /// they stay active into the next scene. The run-scoped arena and player instances are destroyed and
        /// all refs cleared, since this handler is a persistent SubManager and a stale run would break the next.
        /// </summary>
        public override void OnSceneLeave()
        {
            base.OnSceneLeave();
            CleanupRun();
        }

        private void CleanupRun()
        {
            if (CurrentArena != null)
            {
                CurrentArena.DeactivateZone(); // stop waves + worker spawning
                CurrentArena.CleanupZone();    // return every enemy + worker to the pool
                Destroy(CurrentArena.gameObject);
                CurrentArena = null;
            }

            ClearOrbs(); // return xp orbs + currency drops to the pool

            if (Player != null)
            {
                Player.OnDeathEvent -= OnPlayerDeath;
                Destroy(Player.gameObject);
                Player = null;
            }

            AnimationLodModule.PlayerPositionProvider = null; // drop the captured Player reference
            RunState = null;
        }

        public void SetCurrentPhase(RunPhase newPhase)
        {
            if (RunState == null || RunState.Phase == newPhase) return;
            RunState.Phase = newPhase;

            // Play only runs during Running: stop driving the player and freeze the enemy brains.
            bool running = newPhase == RunPhase.Running;
            SetPlayerInputEnabled(running);
            SetEnemiesActive(running);

            OnRunStateChanged?.Invoke(newPhase);
        }

        // Pushes the run's play/pause onto living enemies so their brains freeze off-Running (results screen,
        // restart) instead of each enemy polling the run handler. Phase changes are rare, so the O(n) sweep
        // is cheaper overall than a per-frame per-enemy check — and it leaves enemies test-scene-drivable.
        private void SetEnemiesActive(bool active)
        {
            if (CurrentArena == null || CurrentArena.UnitHandler == null) return;
            HordeEnemyModule.EnemyState state = active ? HordeEnemyModule.EnemyState.Active : HordeEnemyModule.EnemyState.Idle;
            foreach (var enemy in CurrentArena.UnitHandler.AliveEnemies)
            {
                if (enemy == null) continue;
                enemy.GetModule<HordeEnemyModule>()?.SetState(state);
            }
        }

        /// <summary>Current phase — Waiting before a run exists, so callers can query at any time.</summary>
        public RunPhase GetCurrentRunPhase()
        {
            return RunState != null ? RunState.Phase : RunPhase.Waiting;
        }

        private void SetPlayerInputEnabled(bool enabled)
        {
            if (Player == null) return;
            PlayerInputHandler input = Player.GetModule<PlayerInputHandler>();
            if (input != null) input.SetEnabled(enabled);
        }
        /// <summary>
        /// Restarts the run in place (test flow): clears the world, resets state and revives the player,
        /// then restarts the arena from wave 0. Reuses the same arena and player rather than reloading.
        /// </summary>
        public void RestartRun()
        {
            // 1. Stop and clear the current world.
            if (CurrentArena != null)
            {
                CurrentArena.DeactivateZone(); // stop waves + worker spawning, cancel auto-advance
                CurrentArena.CleanupZone();    // despawn all enemies + workers
            }
            ClearOrbs();

            // 2. Reset run state (perk XP, stats).
            ResetRunState();

            // 3. Reset + revive the player in place.
            ResetPlayer();

            // 4. Restart the arena from wave 0.
            if (CurrentArena != null)
            {
                CurrentArena.InitializeArena(Player, ArenaData);
                SubscribeToWaveCompletion();
                CurrentArena.StartArena();
            }

            RunState.IsActive = true;
            SetCurrentPhase(RunPhase.Running); // re-arms enemy brains and player input
            OnRunStarted?.Invoke();
        }

        // Listens for the level's final wave clearing → the win condition. InitializeArena has already
        // resolved the arena's WaveHandler by this point. Re-subscribe safely (guard against doubles).
        private void SubscribeToWaveCompletion()
        {
            if (CurrentArena == null || CurrentArena.WaveHandler == null) return;
            CurrentArena.WaveHandler.OnAllWavesCompleted -= OnLevelCleared;
            CurrentArena.WaveHandler.OnAllWavesCompleted += OnLevelCleared;
        }

        private void OnLevelCleared()
        {
            CompleteRun();
        }

        private void ResetRunState()
        {
            if (RunState == null)
            {
                RunState = new HordeSurvivalRunState(PerkXpConfig);
                RunState.PerkXp.OnLevelUp += HandlePerkLevelUp;
            }
            else
            {
                RunState.PerkXp.Reset();
                RunState.Kills = 0;
                RunState.ElapsedTime = 0f;
                RunState.PendingPerkSelections = 0;
                // Already banked by EndRun — starting fresh, not forfeiting anything.
                RunState.Rewards.Clear();
            }
        }

        private void ResetPlayer()
        {
            if (Player == null) return;
            Vector3 spawnPos = CurrentArena != null ? CurrentArena.transform.position : Vector3.zero;
            Player.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            Player.Spawn(); // ResetActor (resets modules) + state → Spawned (revives)
        }

        private void ClearOrbs()
        {
            DropObject[] orbs = FindObjectsByType<DropObject>(FindObjectsInactive.Exclude);
            foreach (var orb in orbs)
                if (orb != null) PoolManager.PoolObject(orb.gameObject);
        }

        private Actor SpawnPlayer()
        {
            Actor player = PlayerBlueprint.CreateActor();
            player.Spawn();

            // Make it the controlled player.
            ControllerManager cm = GetContext<ControllerManager>();
            if (cm != null && cm.CurrentController != null)
                cm.CurrentController.SetPlayerActor(player);

            // Wire the horde-bonker brain to this run's arena (for nearest worker/warrior lookups).
            HordeBonkerPlayerModule bonker = player.GetModule<HordeBonkerPlayerModule>();
            if (bonker != null) bonker.Arena = CurrentArena;

            // Perk selection draws from the player's perk handler.
            PerkSelection?.AttachActor(player);

            // Apply the permanent talent-tree upgrades (bought with gold) to the fresh player: adds the
            // stat modifiers / granted passives, refreshes derived stats and tops up resources to the new
            // maxima. Once per spawned player — a run always instantiates a fresh player, so no doubling.
            ProgressionManager.ApplyTraitUpgradesToActor(player);

            // Attach the persistent inventory: equipped items apply their components (stat modifiers,
            // attack pattern, animation set) to this run's player, same "meta → fresh player" step as traits.
            PlayerInventoryManager.ApplyInventoryToPlayer(player);

            // Input + camera.
            PlayerInputHandler input = player.GetModule<PlayerInputHandler>();
            if (input != null) input.RegisterInputs();
            if (CameraFollower != null) CameraFollower.Anchor = player.transform;

            //Subscribe to events
            player.OnDeathEvent += OnPlayerDeath;
            return player;
        }
        #endregion

        #region Perk XP (orb-driven)
        /// <summary>
        /// Grants perk XP. Called when the player collects an XP orb (wire the orb → here separately).
        /// </summary>
        public void EarnPerkXp(float amount)
        {
            if (RunState == null || !RunState.IsActive) return;
            RunState.PerkXp.AddValue(amount); // may raise OnLevelUp → HandlePerkLevelUp
            OnPerkXpChanged?.Invoke();
        }

        // A level-up (possibly several at once) queues that many perk picks and offers the first.
        private void HandlePerkLevelUp(object sender, (int oldLevel, int newLevel) levels)
        {
            int gained = Mathf.Max(1, levels.newLevel - levels.oldLevel);
            RunState.PendingPerkSelections += gained;

            OnPerkSelectionAvailable?.Invoke();

            //Open perk panel
            PerkSelectioPanel panel = Core.UI.UIManager.GetPanelById(PerkSelectionPanelId) as PerkSelectioPanel;
            if (panel == null) return;

            var selections = DrawPerks();
            panel.SetCards(selections, ConfirmPerkSelected);
            panel.Open();

            GameManager.PauseGame();
        }

        /// <summary>Draws the perk cards to offer for the current level-up (distinct, weighted, non-maxed).</summary>
        public List<PerkSelectionData> DrawPerks()
        {
            return PerkSelection != null ? PerkSelection.DrawPerkSelection(PerkChoiceCount) : new List<PerkSelectionData>();
        }

        /// <summary>
        /// Call once the player has picked a perk. Applies it, consumes one queued selection, and offers
        /// the next if more are queued (e.g. a multi-level XP burst).
        /// </summary>
        public void ConfirmPerkSelected(PerkSelectionData perk)
        {
            if (RunState == null || RunState.PendingPerkSelections <= 0) return;
            RunState.PendingPerkSelections--;

            PerkSelection?.SelectPerk(perk); // applies via the player's PerkHandlerActorModule

            if (RunState.PendingPerkSelections > 0)
            {
                OnPerkSelectionAvailable?.Invoke();
            }
            GameManager.ResumeGame();
        }

        #endregion

        #region Rewards
        /// <summary>
        /// Adds currency to this run's pending rewards, stacking onto the entry for that currency instead
        /// of piling up one reward per coin — a run drops hundreds of them, but the results screen should
        /// show a single "1,240 gold" line.
        /// </summary>
        public void AddCurrencyReward(CurrencyAsset currency, int amount)
        {
            if (RunState == null || !RunState.IsActive || currency == null || amount <= 0) return;

            string currencyId = currency.GetId();

            // Linear scan: a run pays out a couple of currencies at most, and this keeps display order.
            foreach (var reward in RunState.Rewards)
            {
                if (reward is not CurrencyReward currencyReward || currencyReward.CurrencyId != currencyId) continue;
                currencyReward.CurrencyAmount += amount;
                OnRewardsChanged?.Invoke();
                return;
            }

            RunState.Rewards.Add(new CurrencyReward { CurrencyId = currencyId, CurrencyAmount = amount });
            OnRewardsChanged?.Invoke();
        }

        /// <summary>Adds a non-stacking reward (an item, an unlock) to this run's pending rewards.</summary>
        public void AddReward(Reward reward)
        {
            if (RunState == null || !RunState.IsActive || reward == null) return;
            RunState.Rewards.Add(reward);
            OnRewardsChanged?.Invoke();
        }

        /// <summary>Total of one currency earned so far this run — for the in-run HUD counter.</summary>
        public int GetPendingCurrency(CurrencyAsset currency)
        {
            if (RunState == null || currency == null) return 0;
            string currencyId = currency.GetId();
            foreach (var reward in RunState.Rewards)
                if (reward is CurrencyReward currencyReward && currencyReward.CurrencyId == currencyId)
                    return currencyReward.CurrencyAmount;
            return 0;
        }

        // Banks everything the run earned. Called once from EndRun, so CurrencyManager (which saves on
        // every write) is touched once per currency per run rather than once per pickup.
        private void EarnRewards()
        {
            if (RunState == null) return;
            foreach (var reward in RunState.Rewards)
                reward?.EarnReward();
        }
        #endregion

        private void Update()
        {
            if (RunState != null && RunState.IsActive)
                RunState.ElapsedTime += Time.deltaTime;
        }

        #region Event Handlers
        private void OnPlayerDeath(Actor deadActor)
        {
            FailRun();
        }
        #endregion

#if KUANTECH_DEBUG
        // Bound from PlayerInputHandler (shares its InputSystem_Actions instance, so this only fires while
        // player input is actually enabled — i.e. during an active run, never before RunState exists).
        public void DebugLevelUpPerk()
        {
            if (RunState == null || !RunState.IsActive) return;
            RunState.PerkXp.LevelUp();
        }
#endif
    }
}
