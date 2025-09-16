using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class WaveGenerationDebugger : MonoBehaviour
    {
        [Header("General Parameters")]
        public int DifficultyLevel = 0;   // linear difficulty index (aka level difficulty)
        public int WaveCount = 6;
        public int WaveIndex = 0; // which wave to probe inside the generated series

        [Header("Configs")]
        public WaveGeneratorConfig WaveGeneratorConfig;
        public SpawnablesCollection SpawnablesCollection;

        [Header("Optional Seed Salt (overrides config.SeedSalt just for probe)")]
        public int? SeedSaltOverride = null;

       [Button("Check Wave Params (budget/delay/mix/conc/power)")]
        public void CheckWaveParams()
        {
            if (WaveGeneratorConfig == null)
            {
                Debug.LogWarning("[WG-Debug] Missing WaveGeneratorConfig.");
                return;
            }

            var lp = WaveGenerator.ComputeLevelParams(WaveGeneratorConfig, DifficultyLevel);
            var wp = WaveGenerator.ComputeWaveParams(WaveGeneratorConfig, lp, WaveIndex, WaveCount); 

            var sb = new StringBuilder(256);
            sb.AppendLine($"[WG-Debug] Diff={DifficultyLevel} Wave={WaveIndex}");
            sb.AppendLine($"PowerLevel={lp.PowerLevel}");
            sb.AppendLine($"Budget(Base)={lp.BaseBudget}  IntraMul→ Budget={wp.Budget}");
            sb.AppendLine($"Delay={wp.Delay:0.00}s   Mix={wp.Mix:0.00}   MaxConc={wp.MaxConcurrent}   Chain={lp.ChainChance:0.00}");
            sb.AppendLine($"t01(intra)={wp.T01:0.00}");
            Debug.Log(sb.ToString());
        }

        [Button("Preview Wave Entries (single wave)")]
        public void PreviewWaveEntries()
        {
            if (WaveGeneratorConfig == null || SpawnablesCollection == null)
            {
                Debug.LogWarning("[WG-Debug] Missing config or spawnables.");
                return;
            }

            // Level params + wave params
            var lp = WaveGenerator.ComputeLevelParams(WaveGeneratorConfig, DifficultyLevel);
            var wp = WaveGenerator.ComputeWaveParams(WaveGeneratorConfig, lp, WaveIndex, WaveCount);

            // Tek wave generate (Generate’i kullanırken waveCount=1 ver)
            DebugReport rep;
            var waves = WaveGenerator.Generate(
                WaveGeneratorConfig,
                SpawnablesCollection,
                linearLevelIndex: DifficultyLevel,
                waveCount: 1
            );

            var w = waves != null && waves.Count > 0 ? waves[0] : null;
            if (w == null)
            {
                Debug.LogWarning("[WG-Debug] Preview failed (no wave).");
                return;
            }

            // Özet yaz
            var counts = new Dictionary<int,int>();
            int total = 0;
            foreach (var e in w.WaveEntries)
            {
                if (!counts.TryGetValue(e.SpawnableIndex, out var c)) c = 0;
                c += e.Amount;
                counts[e.SpawnableIndex] = c;
                total += e.Amount;
            }

            var sb = new StringBuilder(512);
            sb.AppendLine($"[WG-Debug] Preview Wave (Diff={DifficultyLevel}, Wave=0 of 1)");
            sb.AppendLine($"Budget≈{wp.Budget}, Delay={wp.Delay:0.00}s, MaxConc={wp.MaxConcurrent}, Mix={wp.Mix:0.00}, Power={lp.PowerLevel}");
            sb.AppendLine($"Entries={w.WaveEntries?.Count ?? 0}, TotalUnits={total}");
            foreach (var kv in counts.OrderBy(k=>k.Key))
                sb.AppendLine($"  idx {kv.Key} -> x{kv.Value}");
            Debug.Log(sb.ToString());
        }

        [Button("Check Series Params (all waves)")]
        public void CheckSeriesParams()
        {
            if (WaveGeneratorConfig == null)
            {
                Debug.LogWarning("[WG-Debug] Missing WaveGeneratorConfig.");
                return;
            }

            var lp = WaveGenerator.ComputeLevelParams(WaveGeneratorConfig, DifficultyLevel);
            int waveCount = 6; // inspector’dan almak istersen alan ekle

            var sb = new StringBuilder(512);
            sb.AppendLine($"[WG-Debug] Diff={DifficultyLevel}  Power={lp.PowerLevel}  BaseBudget={lp.BaseBudget}");
            for (int w = 0; w < waveCount; w++)
            {
                var wp = WaveGenerator.ComputeWaveParams(WaveGeneratorConfig, lp, w, waveCount);
                sb.AppendLine($"  Wave#{w}: Budget={wp.Budget}  Delay={wp.Delay:0.00}s  Mix={wp.Mix:0.00}  MaxConc={wp.MaxConcurrent}");
            }
            Debug.Log(sb.ToString());
        }


        [Button("Generate & Debug Waves")]
        public void GenerateAndDebugWaves()
        {
            if (WaveGeneratorConfig == null || SpawnablesCollection == null || SpawnablesCollection.Spawnables == null || SpawnablesCollection.Spawnables.Count == 0)
            {
                Debug.LogWarning("[WG-Debug] Missing config or empty spawnables.");
                return;
            }

            // 1) Level-scoped parameters (deterministic seed, power level, base budget, etc.)
            var lp = WaveGenerator.ComputeLevelParams(WaveGeneratorConfig, DifficultyLevel);

            // 2) Actually generate the waves for this difficulty
            var waves = WaveGenerator.Generate(WaveGeneratorConfig, SpawnablesCollection, DifficultyLevel, WaveCount);

            // 3) Build a deterministic RNG to mirror the generator’s early/late pick per wave
            //    NOTE: WaveGenerator.Generate(...) already used lp.Seed internally; we reuse it to compute "allowed tags" for pool preview.
            var rng = new System.Random(lp.Seed);

            var sb = new StringBuilder(2048);
            sb.AppendLine($"[WG-Debug] Generate & Debug — Diff={DifficultyLevel}, Waves={WaveCount}");
            sb.AppendLine($"  Seed={lp.Seed}  PowerLevel={lp.PowerLevel}  BaseBudget={lp.BaseBudget}  MaxConc={lp.MaxConcurrent}");
            sb.AppendLine($"  ChainChance(Level)={lp.ChainChance:0.00}  EarlyLateMix(Level)={lp.EarlyLateMix:0.00}");
            sb.AppendLine(new string('-', 80));

            for (int w = 0; w < waves.Count; w++)
            {
                // Mirror the generator’s per-wave params so we can display Budget/Delay/Mix and the filtered pool
                var wp = WaveGenerator.ComputeWaveParams(WaveGeneratorConfig, lp, w, WaveCount);

                // Determine which allow-list was used (same coin flip the generator does)
                bool useLate = rng.NextDouble() < wp.Mix;
                var allowed = useLate ? WaveGeneratorConfig.LateAllowed : WaveGeneratorConfig.EarlyAllowed;

                // Build the filtered pool like the generator (blueprint + tags + minDifficulty gate)
                var pool = SpawnablesCollection.Spawnables
                    .Where(e => e != null
                                && e.ActorBlueprint != null
                                && HasAny(e.Tags, allowed)
                                && DifficultyLevel >= (e.MinDifficultyLevel <= 0 ? 0 : e.MinDifficultyLevel))
                    .ToList();

                // Summarize the generated wave
                var wave = waves[w];
                int totalEntries = wave.WaveEntries?.Count ?? 0;
                int totalUnits = 0;
                float estimatedSpend = 0;

                // Per-type counts to show a compact composition summary
                var perTypeCount = new System.Collections.Generic.Dictionary<int, int>();

                if (wave.WaveEntries != null)
                {
                    foreach (var e in wave.WaveEntries)
                    {
                        totalUnits += e.Amount;

                        // estimate spend using current cost formula at this level's power
                        var entry = SpawnablesCollection.GetEntry(e.SpawnableIndex);
                        float cost = (entry != null) ? WaveGeneratorConfig.CalculateCost(entry, lp.PowerLevel) : 0;
                        estimatedSpend += cost * e.Amount;

                        if (!perTypeCount.TryGetValue(e.SpawnableIndex, out var cur)) cur = 0;
                        perTypeCount[e.SpawnableIndex] = cur + e.Amount;
                    }
                }

                float leftover = Mathf.Max(0, wp.Budget - estimatedSpend);

                sb.AppendLine($"Wave #{w}");
                sb.AppendLine($"  Budget={wp.Budget}  Spent≈{estimatedSpend}  Left≈{leftover}  MaxConc={wp.MaxConcurrent}  Delay={wp.Delay:0.00}s");
                sb.AppendLine($"  Mix={wp.Mix:0.00} ({(useLate ? "LateAllowed" : "EarlyAllowed")})  PoolCount={pool.Count}  AllowedTags={(allowed == null ? 0 : allowed.Count)}");
                sb.AppendLine($"  Entries={totalEntries}  Units={totalUnits}");

                // Print a compact composition (top N by unit count)
                if (perTypeCount.Count > 0)
                {
                    const int topN = 8;
                    var top = perTypeCount.OrderByDescending(kv => kv.Value).Take(topN);
                    foreach (var kv in top)
                    {
                        var entry = SpawnablesCollection.GetEntry(kv.Key);
                        var name = (entry != null && entry.ActorBlueprint != null) ? entry.ActorBlueprint.GetName() : $"idx {kv.Key}";
                        sb.AppendLine($"    {name} (idx {kv.Key}): {kv.Value} unit");
                    }
                }
                else
                {
                    sb.AppendLine("    (no entries)");
                }

                sb.AppendLine(new string('-', 80));
            }

            Debug.Log(sb.ToString());
        }
        [Button("List Spawnable Costs / DPS / CW (formula)")]
        public void ListSpawnableCostsDpsCw()
        {
            if (WaveGeneratorConfig == null || 
                SpawnablesCollection == null || 
                SpawnablesCollection.Spawnables == null || 
                SpawnablesCollection.Spawnables.Count == 0)
            {
                Debug.LogWarning("[WG-Debug] Missing config or empty spawnables.");
                return;
            }

            int powerLevel = WaveGeneratorConfig.CalculatePowerLevel(DifficultyLevel);

            var sb = new StringBuilder(2048);
            sb.AppendLine($"[WG-Debug] Spawnables @ Difficulty={DifficultyLevel}  PowerLevel={powerLevel}");
            sb.AppendLine("Idx | Name | Cost | DPS | CW | MinDiff | Tags");

            foreach (var e in SpawnablesCollection.Spawnables.Where(x => x != null && x.ActorBlueprint != null))
            {
                int idx = e.SpawnableIndex;
                string name = e.ActorBlueprint.GetName();
                float dps = WaveGeneratorConfig.GetDPS(e.ActorBlueprint, powerLevel);
                float cost = WaveGeneratorConfig.CalculateCost(e, powerLevel);
                float cw = WaveGeneratorConfig.CalculateConcurrencyWeight(e, powerLevel);
                int minDiff = e.MinDifficultyLevel;

                string tags = (e.Tags == null || e.Tags.Count == 0)
                    ? "-"
                    : string.Join(",", e.Tags.Where(t => t != null).Select(t => t.name));

                sb.AppendLine($"{idx:000} | {name} | {cost} | {dps:0.##} | {cw:0.##} | {minDiff} | {tags}");
            }

            Debug.Log(sb.ToString());
        }
        private static bool HasAny(System.Collections.Generic.List<EnemyTagAsset> tags,
            System.Collections.Generic.List<EnemyTagAsset> allow)
        {
            if (allow == null || allow.Count == 0) return true;
            if (tags == null) return false;
            foreach (var t in tags)
                if (t != null && allow.Contains(t)) return true;
            return false;
        }
    }
}
