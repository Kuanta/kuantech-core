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

            while (currentBudget >= minCost && safetyCounter < 5000)
            {
                // Bütçenin yettiği adayları filtrele
                var affordableCandidates = candidates.Where(x => x.Cost <= currentBudget).ToList();
                
                // Hiçbir şeye para yetmiyorsa çık
                if (affordableCandidates.Count == 0) break;

                // Ağırlıklı Rastgele Seçim
                EnemyWeightParams selected = PickWeightedRandom(affordableCandidates, rng);

                tempEntries.Add(new WaveEntry
                {
                    SpawnableIndex = selected.Entry.SpawnableIndex,
                    SpawnerIndex = -1, // Random spawner
                    Amount = 1
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