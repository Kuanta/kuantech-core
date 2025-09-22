using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Kuantech.TowerDefense
{ 
public static class 
    WaveGenerator
{
    // params scaled for level
    public struct LevelParams
    {
        public int LinearLevelIndex;
        public int Seed;
        public int PowerLevel;
        public int BaseBudget;         // runtimeBudgetBase
        public int MaxConcurrent;      // runtimeMaxConc
        public float ChainChance;
        public float EarlyLateMix;     // 0..1 (0=early, 1=late)
    }

    // params scaled for wave
    public struct WaveParams
    {
        public int WaveIndex;
        public float T01;           // intra difficulty t (0..1)
        public float Delay;         // this wave delay
        public float Mix;           // this wave composition mix (0..1)
        public int   Budget;        // this wave budget (rest penalty vs intraBudget uygulanmış)
        public int   MaxConcurrent; // this wave max conc
    }

    /// <summary>
    /// Creates the base parameters for the level with given difficulty
    /// </summary>
    /// <returns></returns>
    public static LevelParams ComputeLevelParams(WaveGeneratorConfig cfg, int linearLevelIndex)
    {
        if (cfg == null) throw new Exception("WaveGeneratorConfig is null.");
        var rp = cfg.GetParamsForLevel(linearLevelIndex);

        // base budget (difficulty’e göre)
        int baseBudget = Mathf.RoundToInt(
            (cfg.BaseBudget + cfg.BudgetGrowth * linearLevelIndex) * rp.BudgetMul
            + rp.BudgetQuadTerm
        );
        baseBudget = Mathf.Max(5, baseBudget);

        // Max concurrency (max amount of enemies on the battle)
        int maxConc = Mathf.RoundToInt(Mathf.Lerp(cfg.MaxConcurrentBase, cfg.MaxConcurrentCap, rp.ConcurrencyT));
        maxConc = Mathf.Clamp(maxConc, cfg.MaxConcurrentBase, cfg.MaxConcurrentCap);

        return new LevelParams
        {
            LinearLevelIndex = linearLevelIndex,
            Seed             = MixSeed(cfg.Seed ^ cfg.SeedSalt, linearLevelIndex),
            PowerLevel       = rp.PowerLevel,
            BaseBudget       = baseBudget,
            MaxConcurrent    = maxConc,
            ChainChance      = Mathf.Lerp(cfg.ChainChance, rp.ChainChance, 0.7f),
            EarlyLateMix     = rp.EarlyLateMix
        };
    }

    #region Tag Filtering

    /// <summary>
    /// For tag filtering
    /// </summary>
    /// <param name="tags"></param>
    /// <param name="allowed"></param>
    /// <returns></returns>
    private static bool PassesStrictAndGate(
        List<EnemyTagAsset> tags,
        List<EnemyTagAsset> allowed)
    {
        if (allowed == null || allowed.Count == 0) return true;   // boşsa serbest
        if (tags == null || tags.Count == 0) return false;
        for (int i = 0; i < tags.Count; i++)
        {
            var t = tags[i];
            if (t == null) continue;
            if (!allowed.Contains(t)) return false;
        }
        return true;
    }
    
    /// <summary>
    /// Final selection weight for a candidate spawnable.
    /// Combines cheap-bias with tag-based rules, duplicate penalty, share caps, opener bonus, scarcity.
    /// </summary>
    private static double ComputeCandidateWeight(
        WaveGeneratorConfig cfg,
        SpawnablesCollection.SpawnableEntry cand,
        float targetSpend,              
        float cost,                        
        float t01,                         
        bool isOpener,                      
        int? lastPickedType,                
        Dictionary<EnemyTagAsset,int> tagUnitCounts,  
        int totalUnits           
    )
    {
        // 1) “cheap bias” (ucuzlara eğilim)
        double w = Math.Pow(
            Math.Max(0.1, (double)targetSpend / Math.Max(1.0, cost)),
            cfg.CheapBiasPower
        );

        // 2) Tag based multiplier
        var tags = cand.Tags;
        if (tags != null && tags.Count > 0)
        {
            double sum = 0.0;
            int used = 0;

            for (int i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                if (tag == null) continue;

                var rule = cfg.FindRule(tag);

                float tagWeight = 1f;
                float maxShare = 1f;
                float openerMul = 1f;
                float scarcityPow = 0f;

                if (rule != null)
                {
                    tagWeight   = rule.BaseWeight * rule.WeightOverT.Evaluate(t01);
                    maxShare    = Mathf.Clamp01(rule.MaxShare);
                    openerMul   = isOpener ? Mathf.Max(0f, rule.OpenerMultiplier) : 1f;
                    scarcityPow = Mathf.Max(0f, rule.ScarcityPower);
                }

                int usedCount = 0;
                // share cap
                if (maxShare < 1f && totalUnits > 0 && tagUnitCounts != null &&
                    tagUnitCounts.TryGetValue(tag, out usedCount))
                {
                    float share = (float)usedCount / totalUnits;
                    if (share >= maxShare * 0.98f) tagWeight = 0f;
                }

                // scarcity
                if (scarcityPow > 0f && totalUnits > 0 && tagUnitCounts != null)
                {
                    tagUnitCounts.TryGetValue(tag, out usedCount);
                    float scarcity = 1f - ((float)usedCount / totalUnits); // 0..1
                    tagWeight *= Mathf.Pow(Mathf.Clamp01(scarcity), scarcityPow);
                }

                tagWeight *= openerMul;

                sum  += tagWeight;
                used += 1;
            }

            if (used > 0)
            {
                w *= Math.Max(0.0, sum / used);
            }
        }

        // 3) duplicate penalty
        if (lastPickedType.HasValue && cand.SpawnableIndex == lastPickedType.Value)
            w *= Math.Max(0.0, cfg.DuplicatePenalty);

        return w;
    }

    #endregion
    
    public static WaveParams ComputeWaveParams(
        WaveGeneratorConfig cfg,
        LevelParams lp,
        int waveIndex,
        int waveCount
    )
    {
        if (cfg == null) throw new Exception("WaveGeneratorConfig is null.");
        waveCount = Mathf.Max(1, waveCount);

        // intra difficulty t (0..1) – dalga içi eğriler için
        float baseProgress = (waveCount <= 1) ? 1f : (waveIndex + 0.5f) / waveCount;
        float t01 = Mathf.Clamp01(cfg.IntraWaveDifficulty.Evaluate(baseProgress));

        // delay
        float d01   = Mathf.Clamp01(cfg.IntraDelay.Evaluate(t01));
        float delay = Mathf.Lerp(cfg.DelayMin, cfg.DelayMax, d01);

        // early/late mix: level tendency (lp.EarlyLateMix) ile intra mix’i harmanla
        float intraMix = Mathf.Clamp01(cfg.IntraEarlyLateMix.Evaluate(t01));
        float mix      = Mathf.Clamp01(Mathf.Lerp(lp.EarlyLateMix, intraMix, 0.6f));

        // budget: base * intraBudget + rest penalty
        float bmul  = Mathf.Max(0.1f, cfg.IntraBudget.Evaluate(t01));
        int budget  = Mathf.RoundToInt(lp.BaseBudget * bmul);
        if (cfg.RestEveryN > 0 && (waveIndex + 1) % cfg.RestEveryN == 0)
            budget = Math.Max(10, budget - cfg.RestPenalty);

        return new WaveParams
        {
            WaveIndex     = waveIndex,
            T01           = t01,
            Delay         = delay,
            Mix           = mix,
            Budget        = budget,
            MaxConcurrent = lp.MaxConcurrent
        };
    }
    
    /// <summary>
    /// Public entry point with ramp support.
    /// You pass a base WaveGeneratorConfig, a LevelRampConfig, and the linearLevelIndex (your difficulty index).
    /// This method derives per-level parameters (budget, concurrency, delay, composition mix, power level, seed)
    /// and delegates to the private GenerateCore(...).
    /// </summary>
    public static List<WaveData> Generate(
    WaveGeneratorConfig config,
    SpawnablesCollection spawnables,
    int linearLevelIndex,
    int waveCount
)
{
    if (config == null) throw new Exception("WaveGeneratorConfig is null.");
    if (spawnables == null || spawnables.Spawnables == null || spawnables.Spawnables.Count == 0)
        throw new Exception("SpawnablesCollection is empty!");

    LevelParams levelParams = ComputeLevelParams(config, linearLevelIndex);


    return GenerateCore(
        config,
        spawnables,
        levelParams,
        waveCount,
        linearLevelIndex,
        seedOverride: levelParams.Seed
    );
}
   

   /// <summary>
    /// Core generator doing the actual wave building. Kept private so callers go through GenerateWithRamp.
    /// </summary>
    private static List<WaveData> GenerateCore(
    WaveGeneratorConfig cfg,
    SpawnablesCollection spawnables,
    LevelParams levelParams,
    int waveCount,
    int linearLevelIndex,
    int? seedOverride = null
)
{
    if (cfg == null) throw new Exception("WaveGeneratorConfig null.");
    if (spawnables == null || spawnables.Spawnables == null || spawnables.Spawnables.Count == 0)
        throw new Exception("SpawnablesCollection is empty!");

    int seed = seedOverride ?? MixSeed(cfg.Seed, linearLevelIndex);
    var rng = new System.Random(seed);

    waveCount = Mathf.Max(1, waveCount);
    var waves = new List<WaveData>(waveCount);

    for (int w = 0; w < waveCount; w++)
    {
        WaveParams waveParams = ComputeWaveParams(cfg, levelParams, w, waveCount);
       
        // ---- Tag allow list
        var allowed = cfg.EarlyMixThreshold < waveParams.Mix ? cfg.LateAllowed : cfg.EarlyAllowed;

        // ---- Pool: blueprint + tag + MinDifficultyLevel gate
        var pool = spawnables.Spawnables
            .Where(e => e != null
                        && e.ActorBlueprint != null
                        && HasAny(e.Tags, allowed)
                        && linearLevelIndex >= (e.MinDifficultyLevel <= 0 ? 0 : e.MinDifficultyLevel))
            .ToList();

        Shuffle(pool, rng);

        if (pool.Count == 0)
            throw new Exception($"Wave {w}: No suitable spawnable (after gating & tags).");

        var wave = new WaveData
        {
            EnemyFactionId = 1,
            WaveActorsLevel = levelParams.PowerLevel,
            WaveEntries    = new List<WaveEntry>(),
            MaxEnemyCount  = waveParams.MaxConcurrent,
            GeneratedEnemyCount = 0,
            EnemyProbabilities  = new EnemyProbabilityData { Values = new List<int>(), Weights = new List<float>() },
            WaveSpawnDelay      = waveParams.Delay,
        };

        float remaining = waveParams.Budget;
        float originalWaveBudget = remaining;
        int spawner = 0;

        
        int entriesLeftGoal = Mathf.Clamp(
            rng.Next(cfg.MinEntriesPerWave, cfg.MaxEntriesPerWave + 1),
            1, 999
        );
        
        var tagUnitCounts = new Dictionary<EnemyTagAsset, int>();
        var typeCounts = new Dictionary<int, int>();
        int totalUnits = 0;
        int? lastPickedType = null;
        
        int entryIndex = 0;
        
        
        while (remaining > 0 && wave.WaveEntries.Count < cfg.MaxWaveEntryCount)
        {
            // --- SAFE WINDOW CHECK ---
            // For the first N entries, we try to pick from EarlyAllowed pool and spend less.
            bool inSafeWindow = entryIndex < cfg.SafeEntriesPerWave;
            
            // 1) Target spend per entry (keep budget for later entries)
            float targetSpend = Mathf.Max(1, remaining / Mathf.Max(1, entriesLeftGoal));
            
            // NEW: dampen target spend for openers (gentler start)
            if (inSafeWindow)
                targetSpend = Mathf.Max(1, targetSpend * Mathf.Clamp01(cfg.SafeEntryBudgetMul));
            
            // 2) Build candidate list; first try <= targetSpend*1.15, fallback to <= remaining
            var softCap = Mathf.Max(1, Mathf.RoundToInt(targetSpend * 1.15f));
            
            // NEW: choose pool for this entry (safe → EarlyAllowed; else full pool)
            var poolForThisEntry = pool;
            if (inSafeWindow)
            {
                var safePool = pool.Where(e => HasAll(e.Tags, cfg.FirstWaveEntriesAllowed)).ToList();
                if (safePool.Count > 0) poolForThisEntry = safePool;
            }
            
            var candidates = new List<(SpawnablesCollection.SpawnableEntry e, float cost, double w)>();
            
            bool isOpener = wave.WaveEntries.Count < cfg.OpenerEntryCount; //Check if first few entries. Can be used to scale down the prob of a certain tag

            foreach (var e in poolForThisEntry)
            {
                float c = cfg.CalculateCost(e, levelParams.PowerLevel);
                if (c <= softCap)
                {
                    double wght = ComputeCandidateWeight(cfg, e, targetSpend, c, waveParams.T01,
                        isOpener, lastPickedType,
                        tagUnitCounts, totalUnits);
                    if (wght > 0) candidates.Add((e, c, wght));
                }
            }

            if (candidates.Count == 0) break;

            // 3) Cheap-biased roulette (stronger than before)
            double sumW = 0;
            var weights = new double[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                var cand = candidates[i];
                int candType = cand.e.SpawnableIndex;

                // base cheap bias ~ (targetSpend / cost)^power
                double wght = System.Math.Pow(
                    System.Math.Max(0.1, (double)targetSpend / System.Math.Max(1, cand.cost)),
                    cfg.CheapBiasPower
                );

                // duplicate penalty (if same as last picked)
                if (lastPickedType.HasValue && candType == lastPickedType.Value)
                    wght *= System.Math.Max(0.0, cfg.DuplicatePenalty);

                // max share guard — if this type already dominates, nuke its weight
                if (cfg.MaxTypeShare > 0f && totalUnits > 0)
                {
                    typeCounts.TryGetValue(candType, out int used);
                    float share = (float)used / totalUnits;
                    if (share >= cfg.MaxTypeShare * 0.98f)// a tiny tolerance
                    {
                        wght = 0.0; // effectively remove from lottery
                    } 
                }

                weights[i] = wght;
                sumW += wght;
            }
            if (sumW <= 0.0)
            {
                // All weights zero (e.g., maxShare blocked everything).
                // Fallback: allow all candidates with equal weight.
                for (int i = 0; i < candidates.Count; i++) weights[i] = 1.0;
                sumW = candidates.Count;
            }

            double r = rng.NextDouble() * sumW, acc = 0.0;
            int pickIdx = 0;
            for (int i = 0; i < weights.Length; i++) { acc += weights[i]; if (r <= acc) { pickIdx = i; break; } }
            var pick = candidates[pickIdx];

            // 4) Amount: cap by MaxBatchSize and concurrency, don’t dump all budget at once
            int maxGroup = Math.Max(1, Mathf.RoundToInt(Mathf.Lerp(waveParams.MaxConcurrent * 0.8f, waveParams.MaxConcurrent, waveParams.T01)));
            float affordable = Math.Max(1, remaining / pick.cost);
            float cap        = Mathf.Min(cfg.MaxBatchSize, maxGroup, affordable);
            if (inSafeWindow)
            {
                cap = Mathf.Min(cap, cfg.MaxSafeBatchSize);
            }
            int amount     = rng.Next(1, (int)cap + 1); // random 1..cap to vary

            wave.WaveEntries.Add(new WaveEntry
            {
                SpawnableIndex = pick.e.SpawnableIndex,
                SpawnerIndex   = (cfg.SpawnerLaneCount <= 0) ? 0 : spawner,
                Amount         = amount
            });
            
            //Increase picked type count
            typeCounts.TryGetValue(pick.e.SpawnableIndex, out int cur);
            cur += amount;
            typeCounts[pick.e.SpawnableIndex] = cur;
            
            //Increase picked tag count
            if (pick.e.Tags != null)
            {
                foreach (var tag in pick.e.Tags)
                {
                    if (tag == null) continue;
                    tagUnitCounts.TryGetValue(tag, out var used);
                    tagUnitCounts[tag] = used + amount;
                }
            }
            
            totalUnits += amount;

            lastPickedType = pick.e.SpawnableIndex;
            

            remaining -= pick.cost * amount;
            entriesLeftGoal = Mathf.Max(1, entriesLeftGoal - 1); // << decrease goal so targetSpend rises
            if (cfg.SpawnerLaneCount > 0) spawner = (spawner + 1) % cfg.SpawnerLaneCount;

            // OPTIONAL: tiny chain if still under targetSpend by a margin
            float chainChance = levelParams.ChainChance;
            if (cfg.SkipChainOnSafeEntries && inSafeWindow)
                chainChance = 0f;
            
            bool underShot = (pick.cost * amount) < (int)(targetSpend * 0.6f);
            if (chainChance > 0f && underShot && rng.NextDouble() < chainChance && remaining >= pick.cost)
            {
                int extraCap = (int)Mathf.Min(cfg.MaxBatchSize, maxGroup, Math.Max(1, remaining / pick.cost));
                if (inSafeWindow) extraCap = Mathf.Min(extraCap, cfg.MaxSafeBatchSize);
                int extra = rng.Next(1, extraCap + 1);
                wave.WaveEntries.Add(new WaveEntry
                {
                    SpawnableIndex = pick.e.SpawnableIndex,
                    SpawnerIndex   = (cfg.SpawnerLaneCount <= 0) ? 0 : spawner,
                    Amount         = extra
                });

                remaining -= pick.cost * extra;
                entriesLeftGoal = Mathf.Max(1, entriesLeftGoal - 1);
                if (cfg.SpawnerLaneCount > 0) spawner = (spawner + 1) % cfg.SpawnerLaneCount;
            }
            
            // advance entry index
            entryIndex++;
            
            // --- OPTIONAL: Variety injection near the end ---
            if (cfg.ForceInjectWhenMonotone)
            {
                int distinct = 0;
                foreach (var kv in typeCounts) if (kv.Value > 0) distinct++;

                bool lowVariety = distinct < cfg.MinDistinctTypesPerWave;
                bool waveAlmostDone =
                    remaining <= (int)(originalWaveBudget * 0.2f) ||
                    wave.WaveEntries.Count >= (cfg.MaxWaveEntryCount - 2);

                if (lowVariety && waveAlmostDone)
                {
                    // find a different type than lastPickedType with smallest cost
                    SpawnablesCollection.SpawnableEntry inject = null;
                    float injectCost = float.MaxValue;

                    foreach (var e in pool)
                    {
                        if (lastPickedType.HasValue && e.SpawnableIndex == lastPickedType.Value) continue;

                        float c = cfg.CalculateCost(e, levelParams.PowerLevel);
                        if (c <= remaining && c < injectCost)
                        {
                            // also respect max share guard
                            typeCounts.TryGetValue(e.SpawnableIndex, out int used);
                            float share = (totalUnits > 0) ? (float)used / totalUnits : 0f;
                            if (share >= cfg.MaxTypeShare * 0.98f) continue;

                            inject = e;
                            injectCost = c;
                        }
                    }

                    if (inject != null)
                    {
                        int extraAmount = (int)Mathf.Clamp(cfg.InjectAmount, 1, Mathf.Max(1, remaining / injectCost));
                        wave.WaveEntries.Add(new WaveEntry
                        {
                            SpawnableIndex = inject.SpawnableIndex,
                            SpawnerIndex   = (cfg.SpawnerLaneCount <= 0) ? 0 : spawner,
                            Amount         = extraAmount
                        });

                        // update counters
                        typeCounts.TryGetValue(inject.SpawnableIndex, out int cur2);
                        cur2 += extraAmount;
                        typeCounts[inject.SpawnableIndex] = cur2;
                        totalUnits += extraAmount;

                        remaining -= injectCost * extraAmount;
                        if (cfg.SpawnerLaneCount > 0) spawner = (spawner + 1) % cfg.SpawnerLaneCount;

                        lastPickedType = inject.SpawnableIndex;

                
                    }
                }
            }
        }
        waves.Add(wave);
    }

    return waves;
}

    
    private static bool HasAny(List<EnemyTagAsset> tags, List<EnemyTagAsset> allow)
    {
        if (allow == null || allow.Count == 0) return true;
        if (tags == null) return false;
        foreach (var t in tags)
            if (t != null && allow.Contains(t)) return true;
        return false;
    }

    private static bool HasAll(List<EnemyTagAsset> enemyTags, List<EnemyTagAsset> allowed)
    {
        if (enemyTags == null || enemyTags.Count == 0) return false;        // Reject enemy with no tag
        if (allowed == null || allowed.Count == 0) return true; 
        foreach (var t in enemyTags)
            if (t == null || !allowed.Contains(t)) return false; // If even a single tag isn't allowed, reject
        return true;
    }
    
    private static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static int MixSeed(int seed, int level)
    {
        unchecked
        {
            int h = seed;
            h ^= level * 19349663;
            return h;
        }
    }
    
    
    
    // Optional helper to print to console
    public static void LogReport(DebugReport r)
    {
        if (r == null) { Debug.LogWarning("[WaveGen] (null report)"); return; }
        var sb = new System.Text.StringBuilder(512);
        sb.AppendLine($"[WaveGen] LDI={r.LinearLevelIndex} PL={r.PowerLevel} Seed={r.Seed}");
        sb.AppendLine($"  BudgetBase={r.RuntimeBudgetBase}  MaxConc={r.RuntimeMaxConcurrent}  Delay={r.RuntimeDelay:0.00}s  Chain={r.RuntimeChainChance:0.00}  Mix={r.RuntimeEarlyLateMix:0.00}");
        sb.AppendLine($"  PoolCount={r.PoolCount} (allowedTags={r.AllowedTagCount})");

        foreach (var w in r.Waves)
        {
            sb.AppendLine($"  Wave#{w.WaveIndex}  StartBudget={w.StartBudget}  End={w.EndRemaining}  MaxConc={w.MaxConcurrent}  Delay={w.Delay:0.00}s");
            foreach (var e in w.Entries)
                sb.AppendLine($"    idx {e.SpawnableIndex}  cost {e.CostUsed}  amt {e.Amount}  sp {e.SpawnerIndex}");
        }
        Debug.Log(sb.ToString());
    }
}


public class DebugReport
{
    // Per-level (call) overview
    public int LinearLevelIndex;
    public int PowerLevel;
    public int Seed;
    public int RuntimeBudgetBase;
    public int RuntimeMaxConcurrent;
    public float RuntimeDelay;
    public float RuntimeChainChance;
    public float RuntimeEarlyLateMix;

    // After filtering
    public int AllowedTagCount;   // how many tags we allowed (from config)
    public int PoolCount;         // how many spawnables passed filters

    public List<WaveDebug> Waves = new List<WaveDebug>();

    public class WaveDebug
    {
        public int WaveIndex;
        public int StartBudget;
        public int EndRemaining;
        public int MaxConcurrent;
        public float Delay;

        public List<EntryDebug> Entries = new List<EntryDebug>();
    }

    public class EntryDebug
    {
        public int SpawnableIndex;
        public int CostUsed;
        public int Amount;
        public int SpawnerIndex;
    }
}


}
