using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Core.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// Owns the campaign: arena data loaded from Arenas.json (via JsonDataManager, same pattern as
    /// Skills.json/Chests.json), and the player's linear unlock progress. Each ArenaData row is a single
    /// (arena layout, tier) entry, self-contained (identity, prefab id, difficulty) -- see ArenaData's own
    /// doc comment for why RunData/nesting was dropped.
    ///
    /// Storage is id-based (ArenaGroupId -> Tier -> ArenaData), not index-based -- matches how every other
    /// balance-data lookup in this project resolves (chests by id, items by id, skills by id). ArenaIndex is
    /// still exposed for the existing linear-progression API (SelectedArenaIndex, IsArenaUnlocked, ...): it's
    /// derived from the JSON list's own order (first appearance of each ArenaGroupId), so authoring order in
    /// Arenas.json IS the campaign order -- no separate "index" needs to be authored by hand.
    /// </summary>
    public class ArenaManager : SubManager
    {
        [Header("Arena Prefabs")]
        [Tooltip("Every arena prefab a run can launch. Each is looked up by its own Arena.ArenaId, which is " +
                 "what ArenaData.ArenaPrefabId references -- arenas resolve here rather than through the " +
                 "generic AssetCollection, since this manager already owns everything else arena-related.")]
        [SerializeField] private List<Arena> ArenaPrefabs = new List<Arena>();

        // arenaGroupId -> tier -> that tier's full ArenaData.
        private Dictionary<string, Dictionary<int, ArenaData>> _arenasByGroupAndTier;
        // Campaign order: index i's group id, built from Arenas.json's own array order.
        private List<string> _groupOrder;
        // arenaId -> the prefab registered under it, built from ArenaPrefabs.
        private Dictionary<string, Arena> _arenaPrefabsById;

        // The (arena, tier) the player will launch — set by the arena-select UI (defaults to 0,0), read by the
        // play flow after the game scene loads. This manager is DontDestroy, so the selection survives the
        // scene transition without threading it through scene-transition data.
        [NonSerialized] public int SelectedArenaIndex;
        [NonSerialized] public int SelectedTier;

        public UnityAction OnSelectedArenaChange;

        // arenaIndex -> how many of its tiers are cleared. Linear: clearing tier T implies tiers 0..T are done.
        [SaveableField] private Dictionary<int, int> _completedTiers = new Dictionary<int, int>();

        public void SetSelected(int arenaIndex, int tier)
        {
            SelectedArenaIndex = arenaIndex;
            SelectedTier = tier;
            OnSelectedArenaChange?.Invoke();
        }

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            // Prefab list is serialized on this manager, so it needs nothing external -- unlike the arena
            // data, which waits for JsonDataManager in OnSubmanagersInitialized.
            BuildArenaPrefabLookup();
        }

        // Arenas.json is loaded by JsonDataManager, whose own Initialize() awaits the fetch (remote-or-local)
        // before returning -- so by the time every manager's Initialize() has finished (the
        // OnSubmanagersInitialized invariant), its data is guaranteed ready. Same reasoning as
        // ChestManager/SkillBalancer.
        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            LoadArenas();
        }

        private void LoadArenas()
        {
            _arenasByGroupAndTier = new Dictionary<string, Dictionary<int, ArenaData>>();
            _groupOrder = new List<string>();

            ArenaDataCollection collection = JsonDataManager.GetData<ArenaDataCollection>();
            if (collection?.Arenas == null)
            {
                Debug.LogWarning("[ArenaManager] No arena data available from JsonDataManager.");
                return;
            }

            foreach (var arena in collection.Arenas)
            {
                if (string.IsNullOrEmpty(arena.ArenaGroupId)) continue;

                if (!_arenasByGroupAndTier.TryGetValue(arena.ArenaGroupId, out var tiers))
                {
                    tiers = new Dictionary<int, ArenaData>();
                    _arenasByGroupAndTier[arena.ArenaGroupId] = tiers;
                    _groupOrder.Add(arena.ArenaGroupId); // first appearance = campaign order
                }
                tiers[arena.Tier] = arena;
            }
        }

        // Indexes the authored prefab list by each arena's own id. Ids live on the Arena component rather
        // than beside the reference here, so a prefab carries its identity wherever it is referenced from.
        private void BuildArenaPrefabLookup()
        {
            _arenaPrefabsById = new Dictionary<string, Arena>();
            if (ArenaPrefabs == null) return;

            foreach (Arena prefab in ArenaPrefabs)
            {
                if (prefab == null) continue;

                if (string.IsNullOrEmpty(prefab.ArenaId))
                {
                    Debug.LogWarning($"[ArenaManager] Arena prefab '{prefab.name}' has no ArenaId, so no " +
                                     "ArenaData row can resolve to it.");
                    continue;
                }

                if (_arenaPrefabsById.ContainsKey(prefab.ArenaId))
                {
                    Debug.LogWarning($"[ArenaManager] Duplicate ArenaId '{prefab.ArenaId}' -- prefab " +
                                     $"'{prefab.name}' is ignored, the first one registered wins.");
                    continue;
                }

                _arenaPrefabsById[prefab.ArenaId] = prefab;
            }
        }

        public override void SetDefaultState()
        {
            base.SetDefaultState();
            _completedTiers = new Dictionary<int, int>();
        }

        #region Queries
        public int ArenaCount => _groupOrder != null ? _groupOrder.Count : 0;

        /// <summary>The ArenaGroupId at a campaign position, or null if out of range.</summary>
        public string GetArenaGroupId(int arenaIndex)
            => _groupOrder != null && arenaIndex >= 0 && arenaIndex < _groupOrder.Count ? _groupOrder[arenaIndex] : null;

        /// <summary>The full arena entry for a campaign position (arenaIndex, tier), or null if out of range.</summary>
        public ArenaData GetArenaData(int arenaIndex, int tier)
        {
            string groupId = GetArenaGroupId(arenaIndex);
            return groupId != null ? GetArenaData(groupId, tier) : null;
        }

        /// <summary>The full arena entry by its own (ArenaGroupId, Tier), independent of campaign order.</summary>
        public ArenaData GetArenaData(string arenaGroupId, int tier)
        {
            if (_arenasByGroupAndTier == null || string.IsNullOrEmpty(arenaGroupId)) return null;
            if (!_arenasByGroupAndTier.TryGetValue(arenaGroupId, out var tiers)) return null;
            return tiers.TryGetValue(tier, out var data) ? data : null;
        }

        /// <summary>
        /// The arena prefab registered under an id, or null if no authored prefab carries it. This is the
        /// hop from an ArenaData row (which only stores ArenaPrefabId) to the thing a run instantiates.
        /// </summary>
        public Arena GetArenaById(string arenaId)
        {
            if (string.IsNullOrEmpty(arenaId)) return null;
            // Lazily built so a caller that runs before Initialize (editor tooling, tests) still resolves.
            if (_arenaPrefabsById == null) BuildArenaPrefabLookup();
            return _arenaPrefabsById.TryGetValue(arenaId, out Arena prefab) ? prefab : null;
        }

        /// <summary>The arena prefab for a campaign position, resolved through that tier's ArenaData.</summary>
        public Arena GetArenaPrefab(int arenaIndex, int tier)
        {
            ArenaData data = GetArenaData(arenaIndex, tier);
            return data != null ? GetArenaById(data.ArenaPrefabId) : null;
        }

        public int GetTierCount(int arenaIndex)
        {
            string groupId = GetArenaGroupId(arenaIndex);
            if (groupId == null || _arenasByGroupAndTier == null || !_arenasByGroupAndTier.TryGetValue(groupId, out var tiers)) return 0;
            return tiers.Count;
        }
        #endregion

        #region Progression
        public int GetCompletedTierCount(int arenaIndex)
            => _completedTiers != null && _completedTiers.TryGetValue(arenaIndex, out int count) ? count : 0;

        public bool IsTierCompleted(int arenaIndex, int tierIndex)
            => GetCompletedTierCount(arenaIndex) > tierIndex;

        /// <summary>An arena is open once the PREVIOUS arena has all its tiers cleared. The first is always open.</summary>
        public bool IsArenaUnlocked(int index)
        {
            if (index <= 0) return true;
            int previous = index - 1;
            return GetCompletedTierCount(previous) >= GetTierCount(previous);
        }

        /// <summary>A tier is open once its arena is unlocked AND the previous tier of that arena is cleared.</summary>
        public bool IsTierUnlocked(int arenaIndex, int tierIndex)
        {
            if (!IsArenaUnlocked(arenaIndex)) return false;
            if (tierIndex <= 0) return true; // first tier of an unlocked arena is always open
            return IsTierCompleted(arenaIndex, tierIndex - 1);
        }

        /// <summary>
        /// Records a cleared tier (linear: clearing tier T marks tiers 0..T done) and saves. Call from the
        /// run handler's completion so the next tier / arena opens up.
        /// </summary>
        public void CompleteTier(int arenaIndex, int tierIndex)
        {
            if (GetArenaData(arenaIndex, tierIndex) == null) return;
            int cleared = Mathf.Max(GetCompletedTierCount(arenaIndex), tierIndex + 1);
            _completedTiers[arenaIndex] = cleared;
            SaveState();

            // Point the selection at the next challenge so returning to the menu offers it, not a replay.
            // Both branches are guaranteed unlocked (we just cleared their prerequisite). Manual replay is
            // still available from the arena-select screen.
            AdvanceSelection(arenaIndex, tierIndex);
        }

        private void AdvanceSelection(int arenaIndex, int tierIndex)
        {
            if (tierIndex + 1 < GetTierCount(arenaIndex))
                SetSelected(arenaIndex, tierIndex + 1);          // next tier of the same arena
            else if (arenaIndex + 1 < ArenaCount)
                SetSelected(arenaIndex + 1, 0);                  // first tier of the next arena
            // else: last tier of the last arena — campaign cleared, keep the current selection.
        }
        #endregion
    }
}
