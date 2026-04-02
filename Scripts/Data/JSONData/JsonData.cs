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
                    request.SetRequestHeader("Accept", "application/json");
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        json = request.downloadHandler.text;
                    }

                    json = SanitizeJson(json);
                    _data = JsonUtility.FromJson(json, SerializeType);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"JsonData: Remote load error: {e.Message}");
                    json = null;
                }
            }

            // 2️⃣ Fallback to local
            if (!BetterStreamingAssets.FileExists(GetFullFilePath()))
            {
                if (!BetterStreamingAssets.FileExists(GetFullFilePath()))
                {
                    Debug.LogError($"JsonData: File not found locally at {FilePath}");
                    return;
                }

                json = await UniTask.Run(() => BetterStreamingAssets.ReadAllText(FilePath));
                _data = JsonUtility.FromJson(json, SerializeType);
                Debug.Log("JsonData: Loaded from local.");
            }
        }

        public object ReadData()
        {
            if (!BetterStreamingAssets.FileExists(GetFullFilePath()))
            {
                Debug.LogError($"JsonData: File not found locally at {GetFullFilePath()}");
                return null;
            }

            var json =BetterStreamingAssets.ReadAllText(GetFullFilePath());
            return JsonUtility.FromJson(json, SerializeType);
        }
        static string SanitizeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            // 1) UTF-8 BOM (Zero-Width No-Break Space)
            s = s.TrimStart('\ufeff');

            // 2) Google XSSI guard ("])}'" ile başlıyorsa ilk satırı at)
            if (s.StartsWith(")]}'"))
            {
                int nl = s.IndexOf('\n');
                s = nl >= 0 ? s[(nl + 1)..] : string.Empty;
            }

            // 3) Güvenlik: ilk { veya [’ten önceki çöpü at
            int i = 0;
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            if (i < s.Length && s[i] != '{' && s[i] != '[')
            {
                int j = s.IndexOfAny(new[] { '{', '[' }, i);
                if (j > i) s = s[j..];
            }

            return s;
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