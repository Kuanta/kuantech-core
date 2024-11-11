using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Data.Firebase
{
    public class FirestoreManager : SubManager
    {
        private FirebaseFirestore _firestore;

        public override async UniTask Initialize(GameManager gameManager)
        {
            await InitializeFirebase();
            await base.Initialize(gameManager);
        }
        private async UniTask InitializeFirebase()
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                _firestore = FirebaseFirestore.DefaultInstance;
                Debug.Log("Firebase Initialized!");
            }
            else
            {
                Debug.LogError($"Firebase couldn't initialized: {dependencyStatus}");
            }
        }
    }
}