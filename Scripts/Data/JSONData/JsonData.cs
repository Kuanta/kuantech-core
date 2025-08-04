using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Kuantech.Core.Data
{
    [Serializable]
    public class JsonData
    {
        [Header("Remote")] 
        [SerializeField] private string RemoteUrl;
        
        [Header("Local")]
        [SerializeField] private string TypeName; // fully-qualified type name
        [SerializeField] private string FilePath;
        
        [NonSerialized] private object _data;

        public Type SerializeType => Type.GetType(TypeName);
        public object Data => _data;
        
        public async UniTask ReadFileAsync()
        {
            if (string.IsNullOrEmpty(FilePath) || SerializeType == null)
            {
                Debug.LogError("JsonData: FilePath or Type is null.");
                return;
            }

            string json = null;

            // 1️⃣ Remote URL
            if (!string.IsNullOrEmpty(RemoteUrl))
            {
                try
                {
                    using UnityWebRequest request = UnityWebRequest.Get(RemoteUrl);
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        json = request.downloadHandler.text;
                    }
              
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"JsonData: Remote load error: {e.Message}");
                }
            }

            // 2️⃣ Fallback to local
            if (string.IsNullOrEmpty(json))
            {
                if (!BetterStreamingAssets.FileExists(GetFullFilePath()))
                {
                    Debug.LogError($"JsonData: File not found locally at {FilePath}");
                    return;
                }

                json = await UniTask.Run(() => BetterStreamingAssets.ReadAllText(FilePath));
                Debug.Log("JsonData: Loaded from local.");
            }

            _data = JsonUtility.FromJson(json, SerializeType);
        }
        
        public string GetFullFilePath()
        {
            return FilePath;
        }
        
        public T GetData<T> () where T : class
        {
            if (_data is T data)
            {
                return data;
            }
            Debug.LogError($"JsonData: Data is not of type {typeof(T).Name}");
            return null;
        }
    }
}