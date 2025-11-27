using System;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;
using Kuantech.Rpg; // AttributeAsset için

namespace Kuantech.TowerDefense
{
    [System.Serializable]
    public struct TagPhaseRule
    {
        public EnemyTagAsset Tag;
        [Tooltip("0.0 = En baş, 0.5 = Yolun yarısı, 0.9 = Son wave'ler")]
        [Range(0f, 1f)] public float MinWaveProgress; 
    }
    
    [CreateAssetMenu(menuName = "Kuantech/TowerDefense/Wave Config", fileName = "WaveGeneratorConfig")]
    public class WaveGeneratorConfig : ScriptableObject
    {
        [Header("Budget Settings")]
        [Tooltip("Level 1 için temel bütçe puanı.")]
        public int BaseBudget = 100;

        [Tooltip("Her zorluk seviyesi (PowerLevel) başına bütçeye eklenecek puan.")]
        public int BudgetPerLevel = 50; 

        [Tooltip("İki düşman spawnı arasındaki sabit bekleme süresi (Saniye).")]
        public float SpawnInterval = 1.2f; 

        [Header("Scaling")]
        [Tooltip("Her yeni wave, bir öncekine göre yüzde kaç daha fazla bütçeye sahip olsun? (1.1 = %10 artış)")]
        public float WaveDifficultyMultiplier = 1.15f; 

        [Header("Cost Calculation")]
        [Tooltip("Maliyet hesabında (HP * DPS) HP değerini çekmek için kullanılacak Attribute.")]
        public AttributeAsset HealthAttribute; 
        
        [Tooltip("Maliyet sonucunu dengelemek için genel çarpan.")]
        public float CostMultiplier = 0.1f; 

        [Serializable]
        public struct AttributeConcurencyWeightEntry
        {
            public AttributeAsset AttributeAsset;
            public float ReferenceValue; //Will be normalized by this
            public float Weight;
        }
        public List<AttributeConcurencyWeightEntry> AttributeEntries;
        public float ReferenceDPS;
        public float DPSWeight; //Weight of dps for the attributes
        [SerializeReference] public SpawnableDataGetter DataGetter; 
        
        [Header("Tag Phasing")]
        [Tooltip("Hangi Tag'in bölümün hangi yüzdesinden sonra çıkacağını belirle.")]
        public List<TagPhaseRule> TagRules;

        // Helper: To find tag rule quickly
        public float GetMinProgressForTag(EnemyTagAsset tag)
        {
            if (TagRules == null) return 0f;
            foreach (var rule in TagRules)
            {
                if (rule.Tag == tag) return rule.MinWaveProgress;
            }
            return 0f; 
        }
        
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
                float attValue = DataGetter.GetAttributeValue(entry.ActorBlueprint, attVal.AttributeAsset, powerLevel);
                float attRefValue = Mathf.Max(1, attVal.ReferenceValue);
                totalWeight += attVal.Weight * (attValue / attRefValue);
                totalAttWeight += attVal.Weight;
            }

            totalWeight /= (totalAttWeight + DPSWeight); //Normalize by weights
            return Mathf.Max(0.0f, totalWeight);
        }
        
        public float GetDPS(ActorBlueprint ab, int powerLevel)
        {
            //Get dps
            return DataGetter.GetDPS(ab, powerLevel);
        }
    }
}