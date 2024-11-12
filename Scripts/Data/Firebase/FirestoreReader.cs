using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Kuantech.Data.Firebase
{
    public class FirestoreReader : MonoBehaviour
    {
        public string ApiKey;
        public string ProjectId;
        public string IdToken;

        public UnityAction OnDataWrittenSuccesfully;
        public UnityAction OnDataWriteFailed;
        
        #region Authentication
        public async UniTask SignInAsync(string email, string password)
        {
            var json = new JObject
            {
                { "email", email },
                { "password", password },
                { "returnSecureToken", true }
            };

            using (var request = new UnityWebRequest($"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json.ToString());
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest().ToUniTask();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var responseJson = JObject.Parse(request.downloadHandler.text);
                    IdToken = responseJson["idToken"].ToString();
                    Debug.Log("Giriş başarılı, ID Token alındı!");
                }
                else
                {
                    Debug.LogError("Giriş başarısız: " + request.error);
                }
            }
        }
        #endregion

        #region Write Document

        public async UniTask WriteDocument(string collectionName, string documentId, string jsonData)
        {
            string url = GetUrlForDocument(collectionName, documentId);

            var documentJson = new JObject
            {
                { "fields", JObject.Parse(jsonData) } 
            };

            UnityWebRequest request = new UnityWebRequest(url, "PATCH");
            byte[] jsonToSend = Encoding.UTF8.GetBytes(documentJson.ToString());
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {IdToken}");

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                OnDataWrittenSuccesfully?.Invoke();
            }
            else
            {
                Debug.LogError($"Veri yazma başarısız: {request.error}");
                OnDataWriteFailed?.Invoke();
            }
        }
        private string GetUrlForDocument(string collectionName, string documentId)
        {
            return $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/{collectionName}/{documentId}?key={ApiKey}";
        }
        #endregion

        #region Reading

        public UnityAction<JToken> OnCollectionReadSuccesfully;
        public UnityAction OnCollectionReadFailed;
        public async UniTask ReadCollection(string collectionName)
        {
            string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/{collectionName}?key={ApiKey}";

            using (var request = new UnityWebRequest(url, "GET"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var responseJson = JObject.Parse(request.downloadHandler.text);
                    var documents = responseJson["documents"];
                    OnCollectionReadSuccesfully?.Invoke(documents);
                }
                else
                {
                    Debug.LogError("Collection reading failed: " + request.error);
                    OnCollectionReadFailed?.Invoke();
                }
            }
        }

        #endregion
   
    }
}