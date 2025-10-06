using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using IngameDebugConsole;
using UnityEngine;

namespace Kuantech.Core.Data
{
    public class JsonDataManager : SubManager
    {
        public List<JsonData> JsonDatas;

        private Dictionary<Type, JsonData> _jsonDatasByType;
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            BetterStreamingAssets.Initialize();
            // _jsonDatasByType = new Dictionary<Type, JsonData>();
            // List<UniTask> tasks = new List<UniTask>();
            // foreach (var jsonData in JsonDatas)
            // {
            //     tasks.Add(jsonData.ReadFileAsync());
            //     _jsonDatasByType[jsonData.SerializeType] = jsonData;
            // }
            //
            // await tasks;
            await UpdateDatas();
            Debug.Log("Datas Loaded");
        }
        
        public static T GetData<T>() where T : class
        {
            var ctx = GetContext<JsonDataManager>();
            if (ctx == null) return null;
            Type key = typeof(T);
            if (ctx._jsonDatasByType.ContainsKey(key))
            {
                return ctx._jsonDatasByType[key].GetData<T>();
            }
            return null;
        }

        private async UniTask UpdateDatas()
        {
            _jsonDatasByType = new Dictionary<Type, JsonData>();
            List<UniTask> tasks = new List<UniTask>();
            foreach (var jsonData in JsonDatas)
            {
                tasks.Add(jsonData.ReadFileAsync());
                _jsonDatasByType[jsonData.SerializeType] = jsonData;
            }

            await tasks;
        }
        [ConsoleMethod("updateJsonData", "Updates json datas")]
        public static async UniTask UpdateDataFromRemote()
        {
            var ctx = GetContext<JsonDataManager>();
            await ctx.UpdateDatas();
        }
    }
}