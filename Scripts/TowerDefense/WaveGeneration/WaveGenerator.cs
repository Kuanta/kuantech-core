using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public static class WaveGenerator
    {
        // Adayları seçerken geçici olarak verileri tuttuğumuz sınıf
        public class EnemyWeightParams
        {
            public SpawnablesCollection.SpawnableEntry Entry;
            public float Cost;
            public float SelectionWeight;
        }

        /// <summary>
        /// Belirtilen sayıda wave içeren bir liste üretir.
        /// </summary>
        public static List<WaveData> Generate(
            WaveGeneratorConfig config, 
            SpawnablesCollection spawnables, 
            int difficultyLevel, 
            int waveCount)
        {
            List<WaveData> generatedWaves = new List<WaveData>();

            if (config == null || spawnables == null || config.DataGetter == null)
            {
                Debug.LogError("WaveGenerator: Config, Spawnables veya DataGetter eksik!");
                return generatedWaves;
            }

            // ---------------------------------------------------------
            // 1) Aday Havuzunu Oluştur (Create Candidate Pool)
            // ---------------------------------------------------------
            // Bunu döngünün dışında yapıyoruz ki her wave için tekrar tekrar 
            // cost hesabı ve level kontrolü yapmayalım. Performans için önemli.
            List<EnemyWeightParams> candidates = CreateCandidatePool(config, spawnables, difficultyLevel);

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"Level {difficultyLevel} için uygun hiç düşman bulunamadı! Min/Max Level ayarlarını kontrol et.");
                return generatedWaves;
            }

            // ---------------------------------------------------------
            // 2) Wave'leri Üret
            // ---------------------------------------------------------
            for (int w = 0; w < waveCount; w++)
            {
                // Artık wave üretim mantığı ayrı bir metodda
                WaveData wave = GenerateSingleWave(config, candidates, difficultyLevel, w, waveCount);
                generatedWaves.Add(wave);
            }

            return generatedWaves;
        }

        /// <summary>
        /// Tek bir WaveData üretir ve içini doldurur.
        /// </summary>
        public static WaveData GenerateSingleWave(
            WaveGeneratorConfig config, 
            List<EnemyWeightParams> allCandidates, 
            int difficultyLevel, 
            int waveIndex,
            int totalWaves)
        {
            WaveData wave = new WaveData();
            wave.EnemyFactionId = 1;
            wave.WaveActorsLevel = difficultyLevel;
            wave.WaveEntries = new List<WaveEntry>();
            wave.WaveSpawnDelay = config.SpawnInterval;
                        
            // DEPRECATED
            wave.EnemyProbabilities = new EnemyProbabilityData { Values = new List<int>(), Weights = new List<float>() };
            
            float currentProgress = (float)waveIndex / Mathf.Max(1, totalWaves - 1);

            // Sadece "Zamanı Gelmiş" olanları filtrele
            var validCandidatesForThisWave = allCandidates
                .Where(c => IsCandidateAllowedByPhase(c.Entry, currentProgress, config))
                .ToList();
    
            // Eğer filtre sonucu herkes elendiyse (örn: hepsi "Late" tagliyse ve biz baştaysak)
            // Güvenlik önlemi olarak ana listeyi kullan, oyun kilitlenmesin.
            if (validCandidatesForThisWave.Count == 0)
            {
                Debug.LogWarning($"Wave {waveIndex}: Tag kuralları yüzünden uygun düşman kalmadı. Kurallar yoksayılıyor.");
                validCandidatesForThisWave = allCandidates;
            }


            // Bütçe Hesabı: (Baz + (Level * Artış)) * (WaveÇarpanı ^ WaveIndex)
            float baseLevelBudget = config.BaseBudget + (difficultyLevel * config.BudgetPerLevel);
            float currentWaveBudget = baseLevelBudget * Mathf.Pow(config.WaveDifficultyMultiplier, waveIndex);

            // İçeriği doldur
            FillWaveContent(wave, currentWaveBudget, validCandidatesForThisWave);

            return wave;
        }
        private static bool IsCandidateAllowedByPhase(SpawnablesCollection.SpawnableEntry entry, float progress, WaveGeneratorConfig config)
        {
            // Tag listesi yoksa veya boşsa her zaman izin ver
            if (entry.Tags == null || entry.Tags.Count == 0) return true;

            foreach (var tag in entry.Tags)
            {
                // Eğer tag null ise geç
                if (tag == null) continue;

                // Config'den bu tag için gereken min progress değerini al
                float requiredProgress = config.GetMinProgressForTag(tag);

                // Eğer henüz o ilerlemede değilsek REDDET
                if (progress < requiredProgress)
                {
                    return false; 
                }
            }
    
            // Tüm tag'leri kontrol ettik, hiçbiri "henüz erken" demedi.
            return true;
        }
        /// <summary>
        /// Spawnable listesinden levele uygun adayları seçer ve maliyetlerini hesaplar.
        /// </summary>
        public static List<EnemyWeightParams> CreateCandidatePool(
            WaveGeneratorConfig config, 
            SpawnablesCollection spawnables, 
            int difficultyLevel)
        {
            List<EnemyWeightParams> candidates = new List<EnemyWeightParams>();

            foreach (var entry in spawnables.Spawnables)
            {
                if (entry == null || entry.ActorBlueprint == null) continue;

                // Min Level Kontrolü
                if (difficultyLevel < entry.MinDifficultyLevel) continue;

                // Max Level Kontrolü
                if (difficultyLevel > entry.MaxDifficultyLevel) continue;

                // Cost ve Weight Hesabı
                float cost = config.ComputeScore(entry, difficultyLevel);
                float weight = entry.GetSpawnWeight(difficultyLevel, (int)cost);

                if (weight <= 0f) continue;

                candidates.Add(new EnemyWeightParams
                {
                    Entry = entry,
                    Cost = cost,
                    SelectionWeight = weight
                });
            }

            return candidates;
        }

        /// <summary>
        /// Verilen bütçeye göre WaveData'nın entry listesini doldurur.
        /// </summary>
        private static void FillWaveContent(WaveData wave, float budget, List<EnemyWeightParams> candidates)
        {
            System.Random rng = new System.Random();
            List<WaveEntry> tempEntries = new List<WaveEntry>();
            float currentBudget = budget;
            int safetyCounter = 0;

            // En ucuz düşmanın maliyetini bul (döngüden çıkış şartı)
            float minCost = candidates.Min(x => x.Cost);


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
            
 
            
            // NEW: choose pool for this entry (safe → EarlyAllowed; else full pool)
            var poolForThisEntry = pool;
            if (inSafeWindow)
            {
                // Bütçenin yettiği adayları filtrele
                var affordableCandidates = candidates.Where(x => x.Cost <= currentBudget).ToList();
                
                // Hiçbir şeye para yetmiyorsa çık
                if (affordableCandidates.Count == 0) break;

            foreach (var e in poolForThisEntry)
            {
                float c = cfg.CalculateCost(e, levelParams.PowerLevel);
                double wght = ComputeCandidateWeight(cfg, e, targetSpend, c, waveParams.T01,
                    isOpener, lastPickedType,
                    tagUnitCounts, totalUnits);
                if (wght > 0) candidates.Add((e, c, wght));
            }

            if (candidates.Count == 0)
            {
                break;
            }

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

                currentBudget -= selected.Cost;
                safetyCounter++;
            }

            // ---------------------------------------------------------
            // Shuffle (Homojen Dağılım)
            // ---------------------------------------------------------
            Shuffle(tempEntries, rng);
            
            wave.WaveEntries = tempEntries;
            wave.GeneratedEnemyCount = tempEntries.Count;
        }
    }
    }
    }

        private static EnemyWeightParams PickWeightedRandom(List<EnemyWeightParams> list, System.Random rng)
        {
            float totalWeight = list.Sum(x => x.SelectionWeight);
            double randomValue = rng.NextDouble() * totalWeight;
            
            foreach (var item in list)
            {
                if (randomValue < item.SelectionWeight)
                {
                    return item;
                }
                randomValue -= item.SelectionWeight;
            }
            return list.Last(); // Yuvarlama hatası koruması
        }

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            int n = list.Count;
            while (n > 1) 
            { 
                n--; 
                int k = rng.Next(n + 1); 
                (list[k], list[n]) = (list[n], list[k]); 
            }
        }
    }
}