using System.Collections;
using System.Threading;
using UnityEngine;

namespace Kuantech.Utils
{
    public class FpsManager : MonoBehaviour
    {
        [Header("Frame Settings")]
        public int MaxRate = 9999;
        public float TargetFrameRate = 60.0f;
        float currentFrameTime;
        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = MaxRate;
            currentFrameTime = Time.realtimeSinceStartup;
            StartCoroutine(WaitForNextFrameRoutine());
        }
        
        private IEnumerator WaitForNextFrameRoutine()
        {
            while(true)
            {
                yield return new WaitForEndOfFrame();
                currentFrameTime  += 1.0f / TargetFrameRate;
                var t = Time.realtimeSinceStartup;
                var sleepTime = currentFrameTime - t - 0.01f;
                if(sleepTime > 0)
                {
                    Thread.Sleep((int)(sleepTime*1000.0f));
                }
            }
        }
    }
}