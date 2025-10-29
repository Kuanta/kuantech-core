using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.TowerDefense
{

    [Serializable]
    public class TagWeightRule
    {
        public EnemyTagAsset Tag;
        public float BaseWeight = 1f;                   
        public AnimationCurve WeightOverT =                       
            AnimationCurve.Linear(0, 1, 1, 1);
        [Header("Opener bonus (first N entries)")]
        public float OpenerMultiplier = 1.0f;                      
        [Header("Share limit (cap: 0..1; 1=no cap)")]
        [Range(0f, 1f)] public float MaxShare = 1f;                
        [Header("Scarcity boost (0=off)")]
        public float ScarcityPower = 0f;                          
    }
    
    [CreateAssetMenu(menuName = "Kuantech/TowerDefense/Wave Generator Config", fileName = "WaveGeneratorConfig")]
    public class WaveGeneratorConfig : ScriptableObject
    {
        public struct RampParams
        {
            public float BudgetMul;
            public float BudgetQuadTerm;
            public float ConcurrencyT;
            public float ChainChance;
            public float EarlyLateMix;
            public int PowerLevel;
        }
        
        [Header("Determinism")]
        public int Seed = 12345;
        public int SeedSalt = 0;
        
        [Header("Openers (first N entries per wave)")]
        [Min(0)] public int SafeEntriesPerWave = 2;           // first N entries are "safe/openers"
        [Range(0f, 1f)] public float SafeEntryBudgetMul = 0.6f; // spend less per opener entry
        [Min(1)] public int MaxSafeBatchSize = 4;              // smaller batches for openers
        public bool SkipChainOnSafeEntries = true;             // prevent chaining in openers
        public List<EnemyTagAsset> FirstWaveEntriesAllowed;
        
        [Header("Tag Weights")]
        public List<TagWeightRule> TagWeights = new();
        
        [Tooltip("First N entries can get opener bonus")]
        public int OpenerEntryCount = 2;
        
        public bool UseStrictAndGate = false;  
        
        public TagWeightRule FindRule(EnemyTagAsset tag)
            => TagWeights?.Find(r => r.Tag == tag);
        
        #region Ramp Curves
        [Header("Ramp Curves")]
        [Tooltip("Max Level Difficulty")]
        public int TotalLevels = 50;

        [Tooltip("Budget Multiplier (B(i))")]
        public AnimationCurve BudgetMultiplier = AnimationCurve.Linear(0, 1f, 1, 2.0f);
        public float BudgetQuadK = 0.0f; // hafif kavislendirme: +k*i^2

        [Tooltip("Concurrency Progress (C(i)) 0..1")]
        public AnimationCurve ConcurrencyProgress = AnimationCurve.Linear(0, 0f, 1, 1f);

        [Tooltip("Chain Chance")]
        public AnimationCurve ChainChanceCurve = AnimationCurve.Linear(0, 0.4f, 1, 0.6f);

        [Tooltip("Composition Early→Late mix (0..1)")]
        public AnimationCurve EarlyToLateMix = AnimationCurve.Linear(0, 0.3f, 1, 1.0f);
        [Tooltip("Threshold to distinguish early waves to late waves")]
        public float EarlyMixThreshold = 0.2f;

        [Tooltip("Power Level (enemy stat level)")]
        public AnimationCurve PowerLevelCurve = AnimationCurve.Linear(0, 1f, 1, 5f);
    
        [Header("Intra-Level (within a level)")]
        [Tooltip("0 at first wave, 1 at last wave. Drives wave-local difficulty.")]
        public AnimationCurve IntraWaveDifficulty = AnimationCurve.Linear(0, 0f, 1, 1f);
        
        [Tooltip("Early→Late tag mix per wave within level. 0 = prefer EarlyAllowed, 1 = prefer LateAllowed")]
        public AnimationCurve IntraEarlyLateMix = AnimationCurve.Linear(0, 0.0f, 1, 1.0f);

        [Tooltip("Wave delay curve within a level (0..1)")]
        public AnimationCurve IntraDelay = AnimationCurve.EaseInOut(0, 1, 1, 0); 
        
        [Tooltip("Wave budget curve")]
        public AnimationCurve IntraBudget = AnimationCurve.Linear(0, 0.5f, 1, 1.5f);
        #endregion

        float Norm(int i) => TotalLevels <= 1 ? 1f : Mathf.Clamp01(i / Mathf.Max(1f, (float)(TotalLevels - 1)));

        public RampParams GetParamsForLevel(int levelIndex)
        {
            float t = Norm(levelIndex);

            return new RampParams
            {
                BudgetMul = Mathf.Max(0.1f, BudgetMultiplier.Evaluate(t)),
                BudgetQuadTerm = BudgetQuadK * (levelIndex * levelIndex),

                ConcurrencyT = Mathf.Clamp01(ConcurrencyProgress.Evaluate(t)),

                ChainChance = Mathf.Clamp01(ChainChanceCurve.Evaluate(t)),
                EarlyLateMix = Mathf.Clamp01(EarlyToLateMix.Evaluate(t)),

                PowerLevel = CalculatePowerLevel(levelIndex),
            };
        }

        public int CalculatePowerLevel(int difficultyLevel)
        {
            float t = Norm(difficultyLevel);
            return Mathf.Max(0, Mathf.FloorToInt(PowerLevelCurve.Evaluate(t)));
        }

        [Header("Size")]
        [Min(1)] public int MaxWaveEntryCount = 1000;
    
        [Header("Entry shaping")]
        public int MinEntriesPerWave = 4;        // target range for how many entries a wave should have
        public int MaxEntriesPerWave = 15;
        [Min(1)] public int MaxBatchSize = 4;    // hard cap per entry Amount
        [Range(0.5f, 3f)] public float CheapBiasPower = 1.5f; // >1 => stronger bias to cheaper picks
        
        [Header("Budget")]
        public int BaseBudget = 30;
        public int BudgetGrowth = 20;
        public int RestEveryN = 3;
        public int RestPenalty = 20;

        [Header("Concurrency → WaveData.MaxEnemyCount")]
        public int MaxConcurrentBase = 8;
        public int MaxConcurrentCap = 20;
       
        [Tooltip("Spawn delay within a single wave. Delay = Lerp(DelayMin, DelayMax, IntraDelay.Evaluate(t))")]
        public float DelayMin = 1.2f;     // harder/late side
        public float DelayMax = 2.4f; 

        [Header("Composition")]
        public List<EnemyTagAsset> EarlyAllowed;
        public List<EnemyTagAsset> LateAllowed;

        [Header("Variety")]
        [Range(0f, 1f)] public float ChainChance = 0.5f;

        [Header("Spawner Policy")]
        public int SpawnerLaneCount = 1; //How many lanes

        #region Concurency Weight Calculator
        [Serializable]
        public struct AttributeConcurencyWeightEntry
        {
            public AttributeAsset AttributeAsset;
            public float ReferenceValue; //Will be normalized by this
            public float Weight;
        }
        
        [SerializeReference]
        public SpawnableDataGetter DataGetter;
        
        public List<AttributeConcurencyWeightEntry> AttributeEntries;
        public float ReferenceDPS;
        public float DPSWeight; //Weight of dps for the attributes
        
        
        [Header("Concurrency mapping")]
        public float CWBase = 0.6f;
        public Vector2 CWClamp = new Vector2(0.8f, 3.5f);
        public float CWScale = 1.0f; // skor → ağırlık ölçeği (örn 0.2..1.5)

        [Header("Cost mapping")]
        public float CostScale = 10f;
        public Vector2 CostClamp = new Vector2(0.1f, 999);

        //Params to prevent same types keep spawning over and over againb
        [Header("Diversity")] 
        [Tooltip("same type picked back-to-back → weight *= DuplicatePenalty")]
        [Range(0f, 1f)] public float DuplicatePenalty = 0.6f; 
    
        [Tooltip("no single type may exceed this share of total spawned units")]
        [Range(0f, 1f)] public float MaxTypeShare = 0.5f;     
    
        [Tooltip("ensure at least this many different types appear in the wave")]
        [Min(1)] public int MinDistinctTypesPerWave = 3;  
        
        [Tooltip("if the wave is about to end with too little variety, forcibly inject a different type")] 
        public bool ForceInjectWhenMonotone = true;           

        [Tooltip("how many units to inject (small number keeps balance)")]
        [Min(1)] public int InjectAmount = 1;                 
        
        public float ComputeScore(SpawnablesCollection.SpawnableEntry entry, int powerLevel)
        {
            if (entry == null || entry.ActorBlueprint == null) return 0.0f;
            
            float totalWeight = 0f;
            if (DataGetter == null)
            {
                Debug.LogWarning("Data getter is null");
                return 1.0f;
            }
            
            //Get dps
            if (ReferenceDPS < 0.0f) ReferenceDPS = 0.1f;
            float dps = GetDPS(entry.ActorBlueprint, powerLevel);
            totalWeight += DPSWeight*(dps/ReferenceDPS);

            float totalAttWeight = 0.0f;
            foreach (var attVal in AttributeEntries)
            {
                //Get att val
                float attValue = DataGetter.GetAttributrValue(entry.ActorBlueprint, attVal.AttributeAsset, powerLevel);
                float attRefValue = Mathf.Max(1, attVal.ReferenceValue);
                totalWeight += attVal.Weight * (attValue / attRefValue);
                totalAttWeight += attVal.Weight;
            }

            totalWeight /= (totalAttWeight + DPSWeight); //Normalize by weights
            return Mathf.Max(0.0f, totalWeight);
        }
        
        /// <summary>
        /// Calculates the dps of an entry
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="powerLevel"></param>
        /// <returns></returns>
        public float GetDPS(ActorBlueprint ab, int powerLevel)
        {
            //Get dps
            return DataGetter.GetDPS(ab, powerLevel);
        }
        
        /// <summary>
        /// Skor → ConcurrencyWeight (aritchmetic: base + skor*scale → clamp)
        /// </summary>
        public float CalculateConcurrencyWeight(SpawnablesCollection.SpawnableEntry entry, int powerLevel)
        {
            float score = ComputeScore(entry, powerLevel);
            float cw = CWBase + CWScale * score;
            return Mathf.Clamp(cw, CWClamp.x, CWClamp.y);
        }

        /// <summary>
        /// Skor → Cost (lineer; istersen curve ile genişletebiliriz)
        /// </summary>
        public float CalculateCost(SpawnablesCollection.SpawnableEntry entry, int powerLevel)
        {
            float score = ComputeScore(entry, powerLevel);
            return Mathf.Clamp(CostScale * score, CostClamp.x, CostClamp.y);
        }
        #endregion
   
    }
}