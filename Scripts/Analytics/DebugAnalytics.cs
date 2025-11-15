using Cysharp.Threading.Tasks;
using Kuantech.Core;

namespace Kuantech.Analytics
{
    public class DebugAnalytics : SubManager
    {
        private bool _subscribedToEvents = false;

        public bool Debug;
        public override async UniTask Initialize(GameManager gameManager)
        {
            if (_subscribedToEvents) return;
            _subscribedToEvents = true;

            Kuantech.Analytics.Analytics.DebugEnabled = Debug;
            
            Kuantech.Analytics.Analytics.LevelStarted += OnLevelStarted;
            Kuantech.Analytics.Analytics.LevelEnded += OnLevelEnded;
            Kuantech.Analytics.Analytics.WorldLevelStarted += OnWorldLevelStrated;
            Kuantech.Analytics.Analytics.WorldLevelEnded += OnWorldLevelEnded;
            Kuantech.Analytics.Analytics.UpgradePurchasedEvent += OnUpgradePurchasedEvent;
            Kuantech.Analytics.Analytics.UpgradeWithCategoryPurchasedEvent += OnUpgradeWithCategoryPurchased;
        }
        
        private void OnLevelStarted(object sender, LevelStartedEventArgs args)
        {
            UnityEngine.Debug.Log("Level Started: " + args.LevelIndex);
            //TinySauce.OnGameStarted(args.LevelIndex);
        }

        private void OnLevelEnded(object sender, LevelEndedEventArgs args)
        {
            UnityEngine.Debug.Log("Level Ended: " + args.LevelIndex + " Completed: " + args.IsCompleted + " Score: " + args.Score);
            //TinySauce.OnGameFinished(args.IsCompleted, args.Score, args.LevelIndex);
        }

        private void OnWorldLevelStrated(object sender, WorldLevelStartedEventArgs args)
        {
            UnityEngine.Debug.Log("World Level Started: " + args.WorldIndex + " - " + args.LevelIndex);
            //TinySauce.OnGameStarted(args.WorldIndex.ToString(), args.LevelIndex.ToString());
        }

        private void OnWorldLevelEnded(object sender, WorldLevelEndedEventArgs args)
        {
            UnityEngine.Debug.Log("World Level Ended: " + args.WorldIndex + " - " + args.LevelIndex + " Completed: " + args.IsCompleted + " Score: " + args.Score);
            //TinySauce.OnGameFinished(args.IsCompleted, args.Score, args.WorldIndex.ToString(), args.LevelIndex.ToString());
        }

        private void OnUpgradePurchasedEvent(object sender, UpgradePurchasedEventArgs args)
        {
            UnityEngine.Debug.Log("Upgrade Purchased: " + args.UpgradeId + " Level: " + args.Level);
            //TinySauce.OnUpgradeEvent(args.UpgradeId, args.Level);
        }

        private void OnUpgradeWithCategoryPurchased(object sender, UpgradeWithCategoryPurchasedEventArgs args)
        {
            UnityEngine.Debug.Log("Upgrade Purchased: " + args.Category + " - " + args.UpgradeId + " Level: " + args.Level);
            //TinySauce.OnUpgradeEvent(args.Category, args.UpgradeId, args.Level);
        }
        
        
    }
}