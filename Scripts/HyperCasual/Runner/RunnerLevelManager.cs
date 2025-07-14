using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;

namespace Kuantech.Core.HyperCasual.Runner
{
    [Serializable]
    public struct ChunkLayerFormat
    {
        public string PremadeKey;
        public int HorizontalScale;
        public int VerticalScale;
        public List<List<string>> Slots{ get; set;}
        public List<List<float>> RowOffsets;
        public List<List<float>> ColumnOffsets;
        public List<List<string>> CustomParameters;
    }
    
    [Preserve]
    public class PreservedStringEnumConverter : StringEnumConverter
    {
        
    }
    
    [Serializable]
    public struct ChunkFormat
    {
        [JsonConverter(typeof(PreservedStringEnumConverter))]
        public ChunkType ChunkType { get; set;}
        public List<ChunkLayerFormat> Layers{ get; set;}
    }
    
    [Serializable]
    public struct LevelFormat
    {
        public string LevelName;
        public int ColumnCount;
        public List<ChunkFormat> Chunks;
    }
    
    [Serializable]
    public struct LevelDesigns
    {
        public Dictionary<string, List<List<string>>> PremadeLayers;
        public List<LevelFormat> Levels;
    }
    
    public enum ChunkType
    {
        StartChunk = 0,
        EndChunk = 1,
        Corridor = 2,
        LeftTurn = 3,
        RightTurn = 4,
        CorridorShort = 5,
        BossChunk = 6,
    }
    
    /// <summary>
    /// Dictionary to hold chunk prefabs
    /// </summary>
    [Serializable]
    public class LevelChunkDictionary: SerializableDictionary<ChunkType, GameObject>{}
    
    /// <summary>
    /// Dictionary to hold slots
    /// </summary>
    [Serializable]
    public class SlotDictionary : SerializableDictionary<string, GameObject>
    {
    }
    
    public class RunnerLevelManager : LevelManager
    {
        [Header("General")] 
        public bool GeneratedLevels;
        
        [Header("Designs File paths")]
        [SerializeField] private string LevelDesignsFileName = "Levels.json";

        [Header("Empty Level")] 
        [SerializeField] private RunnerLevel EmptyLevelPrefab;

        [Header("Slots")] 
        public SlotDictionary SlotPrefabs = new SlotDictionary();
        
        [FormerlySerializedAs("_levelDesigns")] public LevelDesigns LevelDesigns;

        // public override Level GetLevel(int levelIndex)
        // {
        //     //Instantiate empty level prefab
        //     RunnerLevel runnerLevel = null;
        //     if (GeneratedLevels)
        //     {
        //         runnerLevel = GenerateLevel(levelIndex);
        //     }
        //     else
        //     {
        //         var levelPrefab = LevelDictionary.Count <= levelIndex ? LevelDictionary[LevelDictionary.Count - 1].gameObject : //Get Last element
        //             LevelDictionary[levelIndex].gameObject;
        //         runnerLevel = Instantiate(levelPrefab).GetComponent<RunnerLevel>();
        //         runnerLevel.transform.position = Vector3.zero;
        //         runnerLevel.transform.rotation = Quaternion.identity;
        //         runnerLevel.LevelNumber = levelIndex;
        //     }
        //     if (runnerLevel == null) throw new Exception("Level is null!");
        //     //todo(gameplay): Get power level and chunk count
        //     runnerLevel.SetupLevel();
        //     return runnerLevel;
        // }

        private int GetChunkCount(int levelIndex)
        {
            return Mathf.FloorToInt(levelIndex / (float)RunnerConfig.ChunkPerLevel) + 3; //+2 for start and end chunk + 1 for base middle
        }
        public RunnerLevel GenerateLevel(int levelIndex)
        {
            RunnerLevel runnerLevel = Instantiate(EmptyLevelPrefab.gameObject).GetComponent<RunnerLevel>();
            runnerLevel.LevelNumber = levelIndex;
            runnerLevel.transform.position = Vector3.zero;
            runnerLevel.transform.rotation = Quaternion.identity;
            return runnerLevel;
        }
        public static int GetPowerLevel(int levelIndex)
        {
            return levelIndex;
        }
        #region Editor Methods
        [Button("Assign Slots")]
        public void AssignSlots(string folderPath = "Assets/Kuantech/Prefabs/Levels/LevelSlots/")
        {
#if UNITY_EDITOR
            DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
            FileInfo[] fileInfos = dirInfo.GetFiles();
            SlotPrefabs.Clear();
            foreach (var fileInfo in fileInfos)
            {
                if(fileInfo.Extension == ".meta") continue;
                string prefabPath = folderPath + "/" + fileInfo.Name;
                GameObject prefab = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;
                string key = fileInfo.Name.Replace(".prefab", "");
                SlotPrefabs[key] = prefab;
            }
#endif
        }
        
        public static GameObject InstantiateLevelElement(GameObject prefab)
        {
            if (Application.isPlaying)
            {
                return GameObject.Instantiate(prefab);
            }
#if UNITY_EDITOR
            return PrefabUtility.InstantiatePrefab(prefab) as GameObject;
#endif
            return null;
        }
        #endregion

    }
}