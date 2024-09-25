using System;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Kuantech.Data
{
    public class SheetReader : MonoBehaviour
    {
        [Header("Keys")] 
        [SerializeField] private string SheetId;
        [SerializeField] private string ApiKey;
        //[SerializeField] private string SheetRange;

        public UnityAction<JObject> OnSheetRead;
        public UnityAction OnSheetFailedToRead;
        
        public string GetRequestUrl(string SheetRange)
        {
            return $"https://sheets.googleapis.com/v4/spreadsheets/{SheetId}/values/{SheetRange}?key={ApiKey}";
        }

        public async UniTask GetSheetData(string SheetRange)
        {
            string url = GetRequestUrl(SheetRange);
            try
            {
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    var operation = await request.SendWebRequest().ToUniTask();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Error fetching sheet data: {request.error}");
                        OnSheetFailedToRead?.Invoke();
                    }
                    else
                    {
                        // Parse the JSON response
                        string jsonResponse = request.downloadHandler.text;
                        await ParseSheetData(jsonResponse);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                OnSheetFailedToRead?.Invoke();
                Console.WriteLine(e);
            }
            
        }

        public async UniTask ParseSheetData(string json)
        {
            // Use Newtonsoft.Json to parse JSON response
            JObject data = JObject.Parse(json);
            OnSheetRead?.Invoke(data);
        }
        //
        // #region Utils
        //
        // public static List<float> CommaSeperatedToList(string commaSeperated)
        // {
        //     
        // }
        // #endregion
    }
    
}