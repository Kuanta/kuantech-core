using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
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
        public UnityAction OnSheetWritten;
        public UnityAction OnSheetFailedToWrite;
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

        public async UniTask WriteToSheet(string SheetRange, JArray values)
        {
            string url = $"https://sheets.googleapis.com/v4/spreadsheets/{SheetId}/values/{SheetRange}:append?valueInputOption=USER_ENTERED&key={ApiKey}";
            JObject body = new JObject
            {
                ["range"] = SheetRange,
                ["values"] = values
            };
            string jsonBody = body.ToString();
            string accessToken = await GetAccessToken();
            try
            {
                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    // Add the body to the request
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                    request.timeout = 3;
                    var operation = await request.SendWebRequest().ToUniTask();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Error writing to sheet: {request.error}");
                        OnSheetFailedToWrite?.Invoke();
                    }
                    else
                    {
                        Debug.Log("Data successfully written to Google Sheet.");
                        OnSheetWritten?.Invoke();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                OnSheetFailedToWrite?.Invoke();
            }
        }

        public async UniTask ClearCells(string SheetRange)
        {
            if (service == null) return;
            var request = service.Spreadsheets.Values.Clear(new ClearValuesRequest(), SheetId, SheetRange);
            await request.ExecuteAsync();
        }
        
        #region Auth
        private SheetsService service;
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "Sheet Reader";

        public SheetsService GetService()
        {
            return service;
        }
        
        private async Task<string> GetAccessToken()
        {
            // Google Cloud Console'dan indirdiğiniz JSON dosyasının yolu
            string credentialPath = Path.Combine(Application.streamingAssetsPath, "credentials.json");

            // ClientSecrets'i yükleyin
            ClientSecrets clientSecrets = GoogleClientSecrets.FromFile(credentialPath).Secrets;

            // Kullanıcıdan yetkilendirme isteyin
            UserCredential userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets, 
                new[] { SheetsService.Scope.Spreadsheets },
                "user",
                CancellationToken.None,
                new FileDataStore("TokenStore", true)); 

            // Token süresini kontrol et ve gerekiyorsa yenile
            var token = userCredential.Token;
            var expiresIn = token.ExpiresInSeconds ?? 0;
            var currentTimeUtc = DateTime.UtcNow;

            if (currentTimeUtc >= token.Issued.AddSeconds(expiresIn - 60))  // Token süresi dolmak üzere
            {
                await userCredential.RefreshTokenAsync(CancellationToken.None);
                Debug.Log("Token refreshed.");
            }
            else
            {
                Debug.Log("Token is still valid.");
            }

            return userCredential.Token.AccessToken;
        }
        
        public async void AuthenticateAndInitializeService(bool forceUpdateToken = false)
        {
            string credentialPath = Path.Combine(Application.streamingAssetsPath, "credentials.json");

            using (var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore("TokenStore", true)); // Token'lar burada saklanıyor

                    // Token süresi dolmuşsa yenile
                    if (forceUpdateToken)
                    {
                        await credential.RefreshTokenAsync(CancellationToken.None);
                    }

                    // Google Sheets API hizmetini oluştur
                    service = new SheetsService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ApplicationName,
                    });

                    Debug.Log("Google Sheets API authenticated and service initialized!");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error during Google authentication: {e.Message}");
                }
            }
        }

        #endregion
    }
    
}