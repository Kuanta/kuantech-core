using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;

namespace Kuantech.Data.Firebase
{
    public abstract class FirestoreModel
    {
        // Belirli bir belgeye kayıtlı belge adını almak için override edilebilir bir DocumentId
        public virtual string DocumentId { get; set; }

        // Veriyi bir sözlük yapısına dönüştürmek için abstract metot
        public abstract Dictionary<string, object> ToDictionary();

        // Sözlük yapısından veriyi modele dönüştürmek için abstract metot
        public abstract void FromDictionary(Dictionary<string, object> dict);

        // Veriyi Firestore'a kaydetme
        public async UniTask SaveAsync(string collectionName)
        {
            var firestore = FirebaseFirestore.DefaultInstance;
            var data = ToDictionary();

            try
            {
                await firestore.Collection(collectionName).Document(DocumentId).SetAsync(data);
                UnityEngine.Debug.Log($"{DocumentId} verisi Firestore'a başarıyla kaydedildi.");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Veri kaydedilirken hata oluştu: {e}");
            }
        }

        // Firestore'dan veriyi okuma
        public async UniTask LoadAsync(string collectionName)
        {
            var firestore = FirebaseFirestore.DefaultInstance;

            try
            {
                var snapshot = await firestore.Collection(collectionName).Document(DocumentId).GetSnapshotAsync();
                if (snapshot.Exists)
                {
                    var data = snapshot.ToDictionary();
                    FromDictionary(data);
                    UnityEngine.Debug.Log($"{DocumentId} verisi Firestore'dan başarıyla alındı.");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"{DocumentId} belgesi Firestore'da bulunamadı.");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Veri alınırken hata oluştu: {e}");
            }
        }
    }
}