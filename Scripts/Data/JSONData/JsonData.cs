using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace Kuantech.Core.Data
{
    [Serializable]
    public class JsonData
    {
        [Header("Remote")] 
        [SerializeField] private bool ReadFromRemote = true;
        [SerializeField] private string RemoteUrl;
        [SerializeField] private string RemoteDevUrl; // for testing in editor
        
        [Header("Local")]
        [SerializeField] private string TypeName; // fully-qualified type name
        [SerializeField] private string FilePath;
        [SerializeField] private string DevFilePath;
        
        [NonSerialized] private object _data;

        public Type SerializeType => Type.GetType(TypeName);
        public object Data => _data;
        
        public async UniTask ReadFileAsync()
        {
            if (string.IsNullOrEmpty(GetFullFilePath()) || SerializeType == null)
            {
                Debug.LogError("JsonData: FilePath or Type is null.");
                return;
            }

            string json = null;

            // 1️⃣ Remote URL
            if (!string.IsNullOrEmpty(GetRemoteUrl()) && ReadFromRemote)
            {
                try
                {
                    using UnityWebRequest request = UnityWebRequest.Get(GetRemoteUrl());
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        json = request.downloadHandler.text;
                    }
                    _data = JsonUtility.FromJson(json, SerializeType);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"JsonData: Remote load error: {e.Message}");
                }
            }

            // 2️⃣ Fallback to local
            if (!BetterStreamingAssets.FileExists(GetFullFilePath()))
            {
                Debug.LogError($"JsonData: File not found locally at {GetFullFilePath()}");
                return;
            }

            json = await UniTask.Run(() => BetterStreamingAssets.ReadAllText(GetFullFilePath()));
            Debug.Log("JsonData: Loaded from local.");

            _data = JsonUtility.FromJson(json, SerializeType);
        }
        
        public string GetFullFilePath()
        {
#if DEV_BUILD
            return DevFilePath;
#else
            return FilePath;
#endif
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

        public string GetRemoteUrl()
        {
            #if DEV_BUILD
            return RemoteDevUrl;
            #else
            return RemoteUrl;
#endif
        }
    }
}