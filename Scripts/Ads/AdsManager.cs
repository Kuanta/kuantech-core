using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.Events;

namespace Kuantech.Ads
{
    public class AdsManager: MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
    {
        public bool AdsEnabled = true;
        public string AndroidId = "5215395";
        public string IosId = "5215394";
        public string adUnitIdAndroid = "Interstitial_Android";
        public string adUnitIdIOS = "Interstitial_iOS";
        public string adRewardAndroid = "Rewarded_Android";
        public string adRewardIOS = "Rewarded_IOS";
        public string myAdUnitId;
        public string rewardedAdId;
        public bool adStarted;
        private bool testMode = false;

        public float AdCooldown = 120f;
        private float _lastAdPlayedTime = 0f;
        
        private UnityAction _adFinishedHandler = null;
        private UnityAction _rewardHandler = null;
        public void Initialize()
        {
            _lastAdPlayedTime = -1 * AdCooldown;
#if UNITY_IOS
	Advertisement.Initialize(myGameIdIOS, testMode);
	myAdUnitId = adUnitIdIOS;
    rewardedAdId = adRewardIOS;

#else
            Advertisement.Initialize(AndroidId, testMode, this);
            myAdUnitId = adUnitIdAndroid;
            rewardedAdId = adRewardAndroid;
#endif
            Advertisement.Load(myAdUnitId, this);
        }

        public void ShowAd(UnityAction adCompleteHandler)
        {
            if (!AdsEnabled || !Advertisement.isInitialized  || Time.time - _lastAdPlayedTime < AdCooldown)
            {
                adCompleteHandler?.Invoke();
                return;
            }
            if (adStarted) return;
            adStarted = true;
            _adFinishedHandler = adCompleteHandler;
            _lastAdPlayedTime = Time.time;
            Advertisement.Show(myAdUnitId, this);
        }

        public void ShowRewardedAd(UnityAction adCompleteHandler, UnityAction rewardHandler)
        {
            if (!AdsEnabled || !Advertisement.isInitialized)
            {
                adCompleteHandler?.Invoke();
                return;
            }
            if (adStarted) return;
            _rewardHandler = rewardHandler;
            adStarted = true;
            _adFinishedHandler = adCompleteHandler;
            Advertisement.Show(rewardedAdId, this);
        }

        public void OnUnityAdsReady(string placementId)
        {
            Debug.Log($"{placementId} is ready");
        }

        public void OnUnityAdsDidError(string message)
        {
            Debug.LogError("Error:"+message);
        }

        public void OnUnityAdsDidStart(string placementId)
        {
            Debug.Log($"{placementId} is playing");
        }

        public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
        {
            if (placementId == rewardedAdId && showResult == ShowResult.Finished)
            {
                //Reward player
                Debug.LogError("Player is rewarded");
            }
            adStarted = false;
            _adFinishedHandler?.Invoke();
        }

        public void OnUnityAdsAdLoaded(string placementId)
        {
            Debug.Log("Ads loaded");
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            Debug.Log("Ads failed to load");
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            Debug.Log($"Ads failed to show \n {message} + {error}");
            Complete(placementId);
        }

        public void OnUnityAdsShowStart(string placementId)
        {
            Debug.Log("Ads started");
        }

        public void OnUnityAdsShowClick(string placementId)
        {
            Debug.Log("Ads show click");
        }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            Debug.Log("Ads show complete");
            if(showCompletionState == UnityAdsShowCompletionState.COMPLETED && placementId == rewardedAdId)
            {
                _rewardHandler?.Invoke();
            }
            Complete(placementId);
        }

        public void OnInitializationComplete()
        {
            Debug.Log("Ads initialization is complete");
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            Debug.Log($"Ads initialization is failed \n {message}");
        }

        private void Complete(string placementId)
        {
            _adFinishedHandler?.Invoke();
            adStarted = false;
            _adFinishedHandler = null;
            _rewardHandler = null;
            Advertisement.Load(placementId, this);
        }
    }
}